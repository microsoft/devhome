// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace DevHome.Stub.Controls;

public class ThemedResourceDictionaryCollection : ObservableCollection<ResourceDictionary>, IDictionary, INameScope
{
    private readonly Dictionary<object, ResourceDictionary> _internalDictionary = new Dictionary<object, ResourceDictionary>();

    public object this[object key]
    {
        get => _internalDictionary[key];
        set => _internalDictionary[key] = (ResourceDictionary)value;
    }

    public bool IsFixedSize => false;

    public bool IsReadOnly => false;

    public ICollection Keys => _internalDictionary.Keys;

    public ICollection Values => _internalDictionary.Values;

    public void Add(object key, object value)
    {
        if (!(value is ResourceDictionary resourceDictionary))
        {
            return;
        }

        _internalDictionary.Add(key, resourceDictionary);
        Add(resourceDictionary);
    }

    public bool Contains(object key)
    {
        return _internalDictionary.ContainsKey(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return _internalDictionary.GetEnumerator();
    }

    public void Remove(object key)
    {
        _internalDictionary.Remove(key);
    }

    public object FindName(string name)
    {
        throw null;
    }

    public void RegisterName(string name, object scopedElement)
    {
        throw new NotSupportedException();
    }

    public void UnregisterName(string name)
    {
    }

    public object FindKey(ResourceDictionary resourceDictionary)
    {
        return _internalDictionary.FirstOrDefault(kvp => kvp.Value == resourceDictionary).Key;
    }
}
