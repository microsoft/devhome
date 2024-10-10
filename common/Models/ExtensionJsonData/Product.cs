// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Models.ExtensionJsonData;

public class Product
{
    public required string ProductId { get; set; }

    public required Properties Properties { get; set; }
}
