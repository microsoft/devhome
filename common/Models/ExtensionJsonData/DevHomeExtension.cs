// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Models.ExtensionJsonData;

public class DevHomeExtension
{
    public required ResourceProperties ResourceProperties { get; set; }

    public List<string> SupportedProviderTypes { get; set; } = new();

    public List<ProviderSpecificProperty> ProviderSpecificProperties { get; set; } = new();
}
