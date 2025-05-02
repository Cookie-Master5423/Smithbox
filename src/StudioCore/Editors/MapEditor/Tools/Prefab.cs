﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SoulsFormats;
using StudioCore.Core;
using StudioCore.Editor;
using StudioCore.Scene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StudioCore.Editors.MapEditorNS;

public class PrefabAttributes
{
    private MapEditor Editor;

    public string PrefabName = "";
    public string PrefixSeparator = "[]";
    public ProjectType Type = ProjectType.Undefined;

    public List<string> TagList { get; set; }

    public PrefabAttributes(MapEditor editor)
    {
        Editor = editor;
        Type = Editor.Project.ProjectType;
    }
}

public abstract class Prefab : PrefabAttributes
{
    public abstract bool ImportJson(string path);
    public abstract List<MsbEntity> GenerateMapEntities(MapContainer targetMap);
    public abstract void ExportSelection(string filepath, string name, string tags, ViewportSelection _selection);

    protected abstract IMsb Map();


    public static Prefab New(ProjectType type)
    {
        return type switch
        {
            ProjectType.ER => new Prefab<MSBE>(),
            ProjectType.SDT => new Prefab<MSBS>(),
            ProjectType.DS1 or ProjectType.DS1R => new Prefab<MSB1>(),
            ProjectType.DS2 or ProjectType.DS2S => new Prefab<MSB2>(),
            ProjectType.DS3 => new Prefab<MSB3>(),
            ProjectType.AC6 => new Prefab<MSB_AC6>(),
            ProjectType.BB => new Prefab<MSBB>(),
            _ => null,
        };
    }

    public void ImportToMap(MapContainer targetMap, RenderScene _scene, ViewportActionManager _actionManager, string prefixName = null)
    {
        if (targetMap is null)
        {
            TaskLogs.AddLog($"Failed to create prefab {PrefabName}: Target map is null.", LogLevel.Error);
            return;
        }

        prefixName ??= PrefabName;
        var parent = targetMap.RootObject;
        List<MsbEntity> ents = GenerateMapEntities(targetMap);
        var entries = ents.Select((entity) => entity.WrappedObject as IMsbEntry);

        foreach (var entry in entries)
        {
            RenameWithRefs(entries, entry, prefixName + entry.Name);
        }

        AddMapObjectsAction act = new(targetMap, _scene, ents, true, parent);
        _actionManager.ExecuteAction(act);
    }

    public List<string> GetSelectedPrefabObjects()
    {
        List<string> entNames = new();
        foreach (var entry in PrefabUtils.GetMapMsbEntries(Map()))
        {
            var typeName = entry.GetType().Name;
            entNames.Add($"{typeName} - {entry.Name}");
        }
        return entNames;
    }
}

internal class Prefab<T> : Prefab
    where T : SoulsFile<T>, IMsb, new()
{
    /// <summary>
    /// Bytes of the MSB that stores prefab data.
    /// </summary>
    public byte[] AssetContainerBytes { get; set; }

    [JsonIgnore]
    public T pseudoMap;

    protected override IMsb Map()
    {
        return pseudoMap;
    }

    public PrefabAttributes Attributes() => this;

    void Load(HashSet<MsbEntity> entities)
    {
        pseudoMap = new();
        MapContainer map = new(null);
        var entries = entities.Select(ent => ent.WrappedObject as IMsbEntry);
        foreach (var ent in entities)
        {
            var copy = ent.Clone();
            var entry = ent.WrappedObject as IMsbEntry;
            if (!CFG.Current.Prefab_IncludeEntityID)
            {
                ent.GetType().GetProperty("EntityID")?.SetValue(ent, 0);
            }
            if (!CFG.Current.Prefab_IncludeEntityGroupIDs)
            {
                ent.GetType().GetProperty("EntityGroupIDs")?.SetValue(ent, new uint[] { });
            }

            StripMsbReference(entries, copy.WrappedObject as IMsbEntry);
            map.AddObject(copy);
        }
        map.SerializeToMSB(pseudoMap, Type);
    }

    public override List<MsbEntity> GenerateMapEntities(MapContainer targetMap)
    {
        // Notes for grouped prefabs/scene tree support:
        // * Problem: to retain this information in MSB upon saving/loading, something will need to be saved somewhere. Maybe a meta file?
        // * Make a map entity of the prefab
        // * Add that to ents list
        // * Make the asset objects children of that
        // * Modify scenetree to handle AssetPrefabs.

        IEnumerable<MsbEntity> Entities<Category>(IMsbParam<Category> category, MsbEntityType type, Func<Category, Category> copy)
            where Category : IMsbEntry
        {
            foreach (var part in category.GetEntries())
            {
                // Using the untyped constructor so that the model is not set
                var entity = new MsbEntity(targetMap, copy(part)) { Type = type };
                yield return entity;
            }
        }

        return new IEnumerable<MsbEntity>[] {
            Entities(pseudoMap.Parts, MsbEntityType.Part, p => p.DeepCopy()),
            Entities(pseudoMap.Events, MsbEntityType.Event, p => p.DeepCopy()),
            Entities(pseudoMap.Regions, MsbEntityType.Region, p => p.DeepCopy()),
        }
        .SelectMany(e => e)
        .ToList();
    }

    /// <summary>
    /// Exports AssetPrefab to json file.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public bool Write(string path)
    {
        try
        {
            AssetContainerBytes = pseudoMap.Write();

            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception e)
        {
            MessageBox.Show(
                $"Unable to export Prefab due to the following error:\n\n{e.Message}\n{e.StackTrace}",
                "Prefab export error",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }

    /// <summary>
    /// Imports AssetPrefab info from json file.
    /// </summary>
    /// <returns>Asset Prefab if successful, null otherwise.</returns>
    public override bool ImportJson(string path)
    {
        try
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.PopulateObject(File.ReadAllText(path), this, settings);

            pseudoMap = SoulsFile<T>.Read(AssetContainerBytes);
            return true;
        }
        catch (Exception e)
        {
            MessageBox.Show(
                $"Unable to import Prefab due to the following error:" +
                $"\n\n{e.Message}"
                , "Asset prefab import error"
                , MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }


    /// <summary>
    /// Export
    /// </summary>
    public override void ExportSelection(string filepath, string name, string tags, ViewportSelection _selection)
    {
        Load(_selection.GetFilteredSelection<MsbEntity>());
        if (!PrefabUtils.GetMapMsbEntries(pseudoMap).Any())
        {
            MessageBox.Show("Export failed, nothing in selection could be exported.", "Prefab Error", MessageBoxButtons.OK);
            return;
        }
        PrefabName = name;
        TagList = tags.Split(",").ToList();
        Write(filepath);
    }
}