﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Editors.ModelEditor;

public class ModelPropertyCache
{
    public readonly Dictionary<string, PropertyInfo[]> PropCache = new();

    public ModelPropertyCache()
    { }

    public PropertyInfo[] GetCachedFields(object obj)
    {
        return GetCachedProperties(obj.GetType());
    }

    public PropertyInfo[] GetCachedProperties(Type type)
    {
        if (!PropCache.TryGetValue(type.FullName, out PropertyInfo[] props))
        {
            props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            props = props.OrderBy(p => p.MetadataToken).ToArray();
            PropCache.Add(type.FullName, props);
        }

        return props;
    }
}

