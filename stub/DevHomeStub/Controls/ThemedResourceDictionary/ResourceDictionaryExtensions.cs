// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace DevHome.Stub.Controls;

public static class ResourceDictionaryExtensions
{
    public static ResourceDictionary Clone(this ResourceDictionary resourceDictionary)
    {
        if (resourceDictionary.Source != null)
        {
            return new ResourceDictionary { Source = resourceDictionary.Source };
        }

        using (var ms = new MemoryStream())
        {
            XamlWriter.Save(resourceDictionary, ms);
            ms.Position = 0;
            return (ResourceDictionary)XamlReader.Load(ms);
        }
    }
}
