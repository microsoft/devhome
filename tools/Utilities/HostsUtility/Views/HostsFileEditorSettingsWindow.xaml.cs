// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Services;
using DevHome.HostsFileEditor.ViewModels;
using WinUIEx;

namespace DevHome.HostsFileEditor.Views;

public sealed partial class HostsFileEditorSettingsWindow : WindowEx
{
    public HostsFileEditorSettingsViewModel ViewModel { get; }

    private string WindowTitle { get; set; }

    public HostsFileEditorSettingsWindow()
    {
        ViewModel = HostsFileEditorApp.GetService<HostsFileEditorSettingsViewModel>();
        this.InitializeComponent();

        ExtendsContentIntoTitleBar = true;

        var stringResource = new StringResource(Path.Combine(AppContext.BaseDirectory, "..\\DevHome\\DevHome.HostsFileEditor.pri"), "Resources/Hosts_Settings_Title");
        WindowTitle = stringResource.GetLocalized("Text");
        Title = WindowTitle;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/HostsUILib/Hosts.ico"));
    }
}
