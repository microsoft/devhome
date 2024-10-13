// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Models.ExtensionJsonData;

/// <summary>
/// Root class that will contain the deserialized data located in the
/// src\Assets\ExtensionInformation.json file. Its schema is located in
/// src\Assets\Schemas\ExtensionInformation.schema.json.
/// </summary>
public class DevHomeExtensionContentData
{
    public List<string> ProductIds { get; set; } = new();

    public List<Product> Products { get; set; } = new();
}
