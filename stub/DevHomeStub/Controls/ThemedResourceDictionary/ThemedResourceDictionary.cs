// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace DevHome.Stub.Controls;

#pragma warning disable CA1010 // Generic interface should also be implemented
public class ThemedResourceDictionary : ResourceDictionary
#pragma warning restore CA1010 // Generic interface should also be implemented
{
    private ThemedResourceDictionaryCollection _themeDictionaries;
    private string _currentTheme = "Default";

    public ThemedResourceDictionaryCollection ThemeDictionaries
    {
        get
        {
            if (_themeDictionaries != null)
            {
                return _themeDictionaries;
            }

            _themeDictionaries = new ThemedResourceDictionaryCollection();
            _themeDictionaries.CollectionChanged += OnThemeDictionariesChanged;

            return _themeDictionaries;
        }
    }

    public ThemedResourceDictionary()
    {
        SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
    }

    private bool ChangeTheme(string oldThemeName, string newThemeName)
    {
        if (ThemeDictionaries.Contains(oldThemeName))
        {
            var oldTheme = (ResourceDictionary)ThemeDictionaries[oldThemeName];
            MergedDictionaries.Remove(oldTheme);
        }

        if (!ThemeDictionaries.Contains(newThemeName))
        {
            return false;
        }

        var newTheme = (ResourceDictionary)ThemeDictionaries[newThemeName];
        MergedDictionaries.Add(newTheme.Clone());
        _currentTheme = newThemeName;

        return true;
    }

    private void OnThemeDictionariesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var newDictionary = e.NewItems.OfType<ResourceDictionary>().FirstOrDefault();
                var key = (string)ThemeDictionaries.FindKey(newDictionary);

                if ((key == "Default" && !SystemParameters.HighContrast) || (key == nameof(SystemParameters.HighContrast) && SystemParameters.HighContrast))
                {
                    MergedDictionaries.Add(newDictionary);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                var oldDictionary = e.OldItems.OfType<ResourceDictionary>().FirstOrDefault();
                MergedDictionaries.Remove(oldDictionary);
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                oldDictionary = e.OldItems.OfType<ResourceDictionary>().FirstOrDefault();
                newDictionary = e.NewItems.OfType<ResourceDictionary>().FirstOrDefault();
                MergedDictionaries.Remove(oldDictionary);
                MergedDictionaries.Insert(e.NewStartingIndex, newDictionary);
                break;
            case NotifyCollectionChangedAction.Reset:
                MergedDictionaries.Clear();
                break;
        }
    }

    private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SystemParameters.HighContrast))
        {
            return;
        }

        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(new EventHandler<PropertyChangedEventArgs>(SystemParameters_StaticPropertyChanged), sender, e);
            return;
        }

        ChangeTheme(_currentTheme, SystemParameters.HighContrast ? nameof(SystemParameters.HighContrast) : "Default");
    }

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => base.ToString();

    protected override void OnGettingValue(object key, ref object value, out bool canCache) => base.OnGettingValue(key, ref value, out canCache);
}
