// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Services.WindowsPackageManager.Contracts;

namespace DevHome.Common.Models.ExtensionJsonData;

public class Properties
{
    public required string PackageFamilyName { get; set; }

    public required bool SupportsWidgets { get; set; }

    public IWinGetPackage? WinGetPackage { get; set; }

    public string Description { get; set; } = string.Empty;

    public string PublisherName { get; set; } = string.Empty;

    public string ProductTitle { get; set; } = string.Empty;

    public required List<DevHomeExtension> DevHomeExtensions { get; set; } = new();

    public string GetLocalizedDescription()
    {
        return WinGetPackage?.Description ?? Description;
    }

    public string GetLocalizedPublisherName()
    {
        return WinGetPackage?.PublisherName ?? PublisherName;
    }

    public string GetLocalizedProductTitle()
    {
        return WinGetPackage?.Name ?? ProductTitle;
    }
}
