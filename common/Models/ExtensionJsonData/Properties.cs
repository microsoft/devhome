// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Models.ExtensionJsonData;

public class Properties
{
    public required string PackageFamilyName { get; set; }

    public required bool SupportsWidgets { get; set; }

    public required LocalizedProperties LocalizedProperties { get; set; }

    public required List<DevHomeExtension> DevHomeExtensions { get; set; } = new();
}
