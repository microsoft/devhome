// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Models.ExtensionJsonData;

public class DevHomeExtension
{
    public required LocalizedProperties LocalizedProperties { get; set; }

    public required List<string> SupportedProviderTypes { get; set; } = new();

    public required List<ProviderSpecificProperty> ProviderSpecificProperties { get; set; } = new();
}
