// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        WindowTitle = "Hosts Utility Settings";
    }
}
