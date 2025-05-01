﻿using Hexa.NET.ImGui;
using SoulsFormats;
using StudioCore.Configuration;
using StudioCore.Core.ProjectNS;
using StudioCore.Interface;
using StudioCore.Resources.JSON;
using System.Collections.Generic;
using System.Numerics;
using static SoulsFormats.GPARAM;

namespace StudioCore.Editors.GparamEditorNS;

public class GparamFieldView
{
    public GparamEditor Editor;
    public Project Project;
    public GparamFieldView(Project curPoject, GparamEditor editor)
    {
        Editor = editor;
        Project = curPoject;
    }
    public void Draw()
    {
        ImGui.Begin("Fields##GparamFields");
        Editor.EditorFocus.SetFocusContext(GparamEditorContext.Field);

        Editor.Filters.DisplayFieldFilterSearch();

        ImGui.BeginChild("GparamFieldsSection");

        if (Editor.Selection.HasGroupSelected())
        {
            GPARAM.Param data = Editor.Selection.GetSelectedGparamGroup();

            for (int i = 0; i < data.Fields.Count; i++)
            {
                GPARAM.IField entry = data.Fields[i];

                var isSelected = Editor.Selection.IsFieldSelected(i);

                var name = entry.Key;
                if (CFG.Current.Gparam_DisplayParamFieldAlias)
                {
                    name = Project.GparamData.Meta.GetReferenceName(entry.Key, entry.Name);
                }

                if (Editor.Filters.IsFieldFilterMatch(entry.Name, ""))
                {
                    // Field row
                    if (ImGui.Selectable($@" {name}##{entry.Key}{i}", isSelected))
                    {
                        Editor.Selection.SelectField(i, entry);
                    }

                    // Arrow Selection
                    if (ImGui.IsItemHovered() && Editor.Selection.AutoSelectField)
                    {
                        Editor.Selection.AutoSelectField = false;
                        Editor.Selection.SelectField(i, entry);
                    }
                    if (ImGui.IsItemFocused() && (InputTracker.GetKey(Veldrid.Key.Up) || InputTracker.GetKey(Veldrid.Key.Down)))
                    {
                        Editor.Selection.AutoSelectField = false;
                    }
                }

                // Context Menu
                if (isSelected)
                {
                    if (ImGui.BeginPopupContextItem($"Options##Gparam_Field_Context"))
                    {
                        if (ImGui.Selectable("Target in Quick Edit"))
                        {
                            Editor.QuickEdit.UpdateFieldFilter(Editor.Selection._selectedParamField.Key);

                            ImGui.CloseCurrentPopup();
                        }
                        UIHelper.Tooltip("Add this field to the Field Filter in the Quick Edit window.");

                        if (ImGui.Selectable("Remove"))
                        {
                            Editor.Selection._selectedParamGroup.Fields.Remove(Editor.Selection._selectedParamField);

                            ImGui.CloseCurrentPopup();
                        }
                        UIHelper.Tooltip("Delete the selected row.");

                        ImGui.EndPopup();
                    }
                }
            }

            if (CFG.Current.Gparam_DisplayAddFields)
            {
                ImGui.Separator();

                DisplayMissingFieldSection();
            }
        }

        ImGui.EndChild();

        ImGui.End();
    }

    /// <summary>
    /// Fields list: Add buttons
    /// </summary>
    public void DisplayMissingFieldSection()
    {
        GPARAM.Param data = Editor.Selection.GetSelectedGparamGroup();

        List<GparamFormatMember> missingFields = new List<GparamFormatMember>();

        // Get source Format Reference
        foreach (var entry in Project.GparamData.Meta.Information.list)
        {
            if (entry.id == Editor.Selection._selectedParamGroup.Key)
            {
                foreach (var member in entry.members)
                {
                    bool isPresent = false;

                    foreach (var pField in data.Fields)
                    {
                        if (member.id == pField.Key)
                        {
                            isPresent = true;
                        }
                    }

                    if (!isPresent)
                    {
                        missingFields.Add(member);
                    }
                }
            }
        }

        foreach (var missing in missingFields)
        {
            // Unknown should be skipped
            if (missing.id != "Unknown")
            {
                if (ImGui.Button($"Add##{missing.id}"))
                {
                    AddMissingField(Editor.Selection._selectedParamGroup, missing);
                }
                ImGui.SameLine();
                ImGui.Text($"{missing.name}");
            }
        }
    }

    /// <summary>
    /// Add missing field to target Param Group
    /// </summary>
    /// <param name="targetParam"></param>
    /// <param name="missingField"></param>
    public void AddMissingField(Param targetParam, GparamFormatMember missingField)
    {
        var typeName = Project.GparamData.Meta.GetTypeForProperty(missingField.id);

        if (typeName == "Byte")
        {
            GPARAM.ByteField newField = new GPARAM.ByteField();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<byte>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = 0;

            newField.Values = new List<FieldValue<byte>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "Short")
        {
            GPARAM.ShortField newField = new GPARAM.ShortField();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<short>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = 0;

            newField.Values = new List<FieldValue<short>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "IntA" || typeName == "IntB")
        {
            GPARAM.IntField newField = new GPARAM.IntField();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<int>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = 0;

            newField.Values = new List<FieldValue<int>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "Float")
        {
            GPARAM.FloatField newField = new GPARAM.FloatField();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<float>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = 0;

            newField.Values = new List<FieldValue<float>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "BoolA" || typeName == "BoolB")
        {
            GPARAM.BoolField newField = new GPARAM.BoolField();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<bool>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = false;

            newField.Values = new List<FieldValue<bool>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "Float2")
        {
            GPARAM.Vector2Field newField = new GPARAM.Vector2Field();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<Vector2>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = new Vector2(0f, 0f);

            newField.Values = new List<FieldValue<Vector2>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "Float3")
        {
            GPARAM.Vector3Field newField = new GPARAM.Vector3Field();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<Vector3>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = new Vector3(0f, 0f, 0f);

            newField.Values = new List<FieldValue<Vector3>> { valueList };

            targetParam.Fields.Add(newField);
        }
        if (typeName == "Float4")
        {
            GPARAM.Vector4Field newField = new GPARAM.Vector4Field();
            newField.Key = missingField.id;
            newField.Name = missingField.name;

            // Empty values
            var valueList = new GPARAM.FieldValue<Vector4>();
            valueList.Id = 0;
            valueList.Unk04 = 0;
            valueList.Value = new Vector4(0f, 0f, 0f, 0f);

            newField.Values = new List<FieldValue<Vector4>> { valueList };

            targetParam.Fields.Add(newField);
        }

        // Unknown
    }
}
