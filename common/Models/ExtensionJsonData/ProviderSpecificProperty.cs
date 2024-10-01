// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Models.ExtensionJsonData;

public class ProviderSpecificProperty
{
    public required ResourceProperties ResourceProperties { get; set; }

    public required string ProviderType { get; set; }
}
