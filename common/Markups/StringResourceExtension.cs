// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Markup;

namespace DevHome.Common.Markups;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class StringResourceExtension : MarkupExtension
{
    // TODO https://github.com/microsoft/devhome/issues/1288
    private static readonly Lazy<StringResource> _stringResourceSetupFlow = new(() => new StringResource("DevHome.SetupFlow.pri", "DevHome.SetupFlow/Resources"));

    public string Name { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    protected override object ProvideValue()
    {
        var value = GetStringResource()?.GetLocalized(Name);
        return string.IsNullOrEmpty(value) ? $"{Source}/{Name}" : value;
    }

    private StringResource? GetStringResource()
    {
        return Source switch
        {
            "SetupFlow" => _stringResourceSetupFlow.Value,
            _ => null,
        };
    }
}
