// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.Views;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.Windows;
public sealed partial class InstallationNotesWindow : SecondaryWindowExtension
{
    private readonly InstallationNotesView _installationNotesView;

    private const double InitialHeight = 385;
    private const double InitialWidth = 501;

    public InstallationNotesWindow(string packageTitle, string installationNotes)
    {
        var stringResource = Application.Current.GetService<ISetupFlowStringResource>();
        var title = stringResource.GetLocalized(StringResourceKey.InstallationNotesTitle, packageTitle);

        // Populate content
        _installationNotesView = new (title, installationNotes);
        Title = title;
        Content = _installationNotesView;

        // Configure layout
        ExtendsContentIntoTitleBar = true;
        this.SetIcon(_installationNotesView.DevHomeIconPath);
        SetTitleBar(_installationNotesView.TitleBar);
        UseAppTheme = true;
        Activated += UpdateTitleBarTextColors;

        // Configure dimensions
        MinHeight = InitialHeight;
        Height = InitialHeight;
        MinWidth = InitialWidth;
        Width = InitialWidth;
    }

    public void UpdateTitleBarTextColors(object sender, WindowActivatedEventArgs args)
    {
        var colorBrush = TitleBarHelper.GetTitleBarTextColorBrush(args.WindowActivationState);
        _installationNotesView.UpdateTitleBarTextForeground(colorBrush);
    }
}
