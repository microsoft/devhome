// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;

namespace DevHome.Common.Models.ExtensionJsonData;

public class ResourceProperties
{
    public string DisplayNameKey { get; set; } = string.Empty;

    public string PublisherDisplayNameKey { get; set; } = string.Empty;

    public string ShortDescriptionKey { get; set; } = string.Empty;

    public string LongDescriptionKey { get; set; } = string.Empty;
}
