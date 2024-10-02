// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Models.ExtensionJsonData;

/// <summary>
/// Root class that will contain the deserialized data located in the
/// src\Assets\Schemas\ExtensionInformation.schema.json file.
/// </summary>
public class DevHomeExtensionJsonData
{
    public List<Product> Products { get; set; } = new();
}
