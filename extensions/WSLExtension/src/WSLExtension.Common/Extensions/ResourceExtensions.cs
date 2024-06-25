// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.ApplicationModel.Resources;

namespace WSLExtension.Common.Extensions;

public static class ResourceExtensions
{
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);
}
