// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.SetupFlow.Views;

public sealed partial class InstallationNotesView : Page
{
    public string DevHomeIconPath => Path.Combine(AppContext.BaseDirectory, "Assets/DevHome.ico");

    public string PackageTitle { get; }

    public string InstallationNotes { get; }

    public Grid TitleBar => AppTitleBar;

    public InstallationNotesView(string packageTitle, string installationNotes)
    {
        PackageTitle = packageTitle;
        InstallationNotes = installationNotes;

        this.InitializeComponent();
    }

    public void UpdateTitleBarTextForeground(SolidColorBrush brush)
    {
        AppTitleBarText.Foreground = brush;
    }
}
