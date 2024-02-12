// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Windows;

namespace DevHome.SetupFlow.Windows;

public sealed partial class InstallationNotesWindow : SecondaryWindow
{
    private string PackageTitle { get; }

    private string InstallationNotes { get; }

    public InstallationNotesWindow(string packageTitle, string installationNotes)
    {
        PackageTitle = packageTitle;
        InstallationNotes = installationNotes;
        this.InitializeComponent();
    }
}
