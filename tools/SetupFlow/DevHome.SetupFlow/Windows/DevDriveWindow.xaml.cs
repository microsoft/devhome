// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.Windows;

public sealed partial class DevDriveWindow : SecondaryWindowExtension
{
    private readonly DevDriveViewModel _devDriveViewModel;
    private readonly DevDriveView _devDriveView;
    private const double InitialHeight = 518;
    private const double InitialWidth = 577;

    public DevDriveWindow(DevDriveViewModel viewModel)
        : base()
    {
        _devDriveViewModel = viewModel;
        _devDriveView = new DevDriveView(viewModel);
        this.SetIcon(viewModel.AppImage);
        Title = viewModel.AppTitle;
        Content = _devDriveView;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_devDriveView.TitleBar);
        UseAppTheme = true;
        Activated += UpdateTitleBarTextColors;
        MinHeight = InitialHeight;
        Height = InitialHeight;
        MinWidth = InitialWidth;
        Width = InitialWidth;
    }

    public DevDriveViewModel ViewModel => _devDriveViewModel;

    public void UpdateTitleBarTextColors(object sender, WindowActivatedEventArgs args)
    {
        var colorBrush = TitleBarHelper.GetTitleBarTextColorBrush(args.WindowActivationState);
        _devDriveView.UpdateTitleBarTextForeground(colorBrush);
    }
}
