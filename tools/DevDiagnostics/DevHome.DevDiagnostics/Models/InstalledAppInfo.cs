// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;

namespace DevHome.DevDiagnostics.Models;

public class InstalledAppInfo
{
    public string? Name { get; set; }

    public BitmapImage? Icon { get; set; }

    public string? ShortcutFilePath { get; set; }

    public string? TargetPath { get; set; }

    public string? AppUserModelId { get; set; }

    public Package? AppPackage { get; set; }

    public string? IconFilePath { get; set; }

    public bool IsMsix
    {
        get => AppUserModelId is not null;

        set
        {
        }
    }

    public override string ToString()
    {
        return Name ?? base.ToString()!;
    }
}
