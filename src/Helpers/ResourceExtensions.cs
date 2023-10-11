// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.ApplicationModel.Resources;

namespace DevHome.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader _resourceLoader = new ();

    public static string GetLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);
}
