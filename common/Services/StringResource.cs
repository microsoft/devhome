// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.Common.Services;

public class StringResource : IStringResource
{
    private const int MaxBufferLength = 1024;

    private readonly ResourceLoader _resourceLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringResource"/> class.
    /// <inheritdoc cref="ResourceLoader.ResourceLoader"/>
    /// </summary>
    public StringResource()
    {
        _resourceLoader = new ResourceLoader();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringResource"/> class.
    /// <inheritdoc cref="ResourceLoader.ResourceLoader(string)"/>
    /// </summary>
    /// <param name="name">fsa</param>
    /// <param name="path">path</param>
    public StringResource(string name, string path)
    {
        _resourceLoader = new ResourceLoader(name, path);
    }

    /// <summary>
    /// Gets the localized string of a resource key.
    /// </summary>
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

    /// <summary>
    /// Gets the string of a ms-resource for a given package.
    /// </summary>
    /// <param name="resource">the ms-resource:// path to a resource in an app package's pri file.</param>
    /// <param name="packageFullName">the package containing the resource.</param>
    /// <returns>The retrieved string represented by the resource key.</returns>
    public unsafe string GetResourceFromPackage(string resource, string packageFullName)
    {
        var indirectPathToResource = "@{" + packageFullName + "?" + resource + "}";
        Span<char> outputBuffer = new char[MaxBufferLength];

        fixed (char* outBufferPointer = outputBuffer)
        {
            fixed (char* resourcePathPointer = indirectPathToResource)
            {
                var res = PInvoke.SHLoadIndirectString(resourcePathPointer, new PWSTR(outBufferPointer), (uint)outputBuffer.Length);
                res.ThrowOnFailure();
                return new string(outputBuffer.TrimEnd('\0'));
            }
        }
    }
}
