// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Environments.ViewModels;

public class PropertyViewModel : ICardProperty
{
    public string? Title
    {
        get; private set;
    }

    public object? Value
    {
        get; private set;
    }

    public string? Glyph
    {
        get; private set;
    }

    public BitmapImage? Icon
    {
        get; private set;
    }

    public PropertyViewModel(string title, int value, string glyph)
    {
        Title = title;
        Value = value.ToString(CultureInfo.CurrentCulture);
        Glyph = glyph;
    }
}
