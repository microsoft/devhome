// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace HyperVExtension.Models.VMGalleryJsonToClasses;

/// <summary>
/// Represents a list of image json objects in the VM Gallery. See Gallery Json "https://go.microsoft.com/fwlink/?linkid=851584"
/// </summary>
public sealed class VMGalleryImageList
{
    public List<VMGalleryImage> Images { get; set; } = new List<VMGalleryImage>();

    public string GetJsonDataForAdaptiveCard()
    {
        var choicesList = new List<KeyValuePair<string, string>>();

        for (var i = 0; i < Images.Count; i++)
        {
            choicesList.Add(new KeyValuePair<string, string>(Images[i].Name, $"{i}"));
        }

        return JsonSerializer.Serialize(choicesList);
    }
}
