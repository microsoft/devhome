// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Windows.ApplicationModel.Resources;

namespace HyperVExtension.Common;

public class StringResource : IStringResource
{
    private readonly ResourceLoader _resourceLoader;

    public StringResource()
    {
        _resourceLoader = new ResourceLoader("HyperVExtension/Resources");
    }

    public StringResource(string name)
    {
        _resourceLoader = new ResourceLoader(name);
    }

    /// <summary> Gets the localized string of a resource key.</summary>
    /// <param name="key">Resource key.</param>
    /// <param name="args">Placeholder arguments.</param>
    /// <returns>Localized value, or resource key if the value is empty or an exception occurred.</returns>
    public string GetLocalized(string key, params object[] args)
    {
        string value;

        try
        {
            value = _resourceLoader.GetString(key);
            value = string.Format(CultureInfo.CurrentCulture, value, args);
        }
        catch
        {
            value = string.Empty;
        }

        return string.IsNullOrEmpty(value) ? key : value;
    }
}
