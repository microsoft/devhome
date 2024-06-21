// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Dashboard.Controls;

[ObservableObject]
public sealed partial class DashboardMessage : UserControl
{
    public Messages Message
    {
        get => Message;
        set
        {
            switch (value)
            {
                case Messages.None:
                    MessageText = string.Empty;
                    HyperlinkButtonText = null;
                    HyperlinkButtonCommand = null;
                    break;
                case Messages.NoWidgets:
                    MessageText = _stringResource.GetLocalized("NoWidgetsAddedText");
                    HyperlinkButtonText = null;
                    HyperlinkButtonCommand = null;
                    break;
                case Messages.RunningAsAdmin:
                    MessageText = _stringResource.GetLocalized("RunningAsAdminMessageText");
                    HyperlinkButtonText = null;
                    HyperlinkButtonCommand = null;
                    break;
                case Messages.RestartDevHome:
                    MessageText = _stringResource.GetLocalized("RestartDevHomeMessageText");
                    HyperlinkButtonText = null;
                    HyperlinkButtonCommand = null;
                    break;
                case Messages.UpdateWidgetService:
                    MessageText = _stringResource.GetLocalized("UpdateWidgetsMessageText");
                    HyperlinkButtonText = _stringResource.GetLocalized("UpdateWidgetsMessageLinkText");
                    HyperlinkButtonCommand = GoToWidgetsInStoreCommand;
                    break;
                default:
                    MessageText = string.Empty;
                    HyperlinkButtonText = null;
                    HyperlinkButtonCommand = null;
                    break;
            }
        }
    }

    public enum Messages
    {
        None,
        NoWidgets,
        RunningAsAdmin,
        RestartDevHome,
        UpdateWidgetService,
    }

    private readonly StringResource _stringResource;

    [ObservableProperty]
    private string _messageText;

    ////[ObservableProperty]
    ////private HyperlinkButton _messageHyperlinkButton;

    [ObservableProperty]
    private string _hyperlinkButtonText;

    [ObservableProperty]
    private IAsyncRelayCommand _hyperlinkButtonCommand;

    [ObservableProperty]
    private bool _useAddCreateHyperlinkButton;

    public DashboardMessage()
    {
        this.InitializeComponent();
        _stringResource = new StringResource("DevHome.Dashboard.pri", "DevHome.Dashboard/Resources");
    }

    [RelayCommand]
    public async Task GoToWidgetsInStoreAsync()
    {
        if (Common.Helpers.RuntimeHelper.IsOnWindows11)
        {
            await Windows.System.Launcher.LaunchUriAsync(new($"ms-windows-store://pdp/?productid={WidgetHelpers.WebExperiencePackPackageId}"));
        }
        else
        {
            await Windows.System.Launcher.LaunchUriAsync(new($"ms-windows-store://pdp/?productid={WidgetHelpers.WidgetServiceStorePackageId}"));
        }
    }
}
