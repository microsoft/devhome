// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Timers;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.Views;

public partial class SetupFlowPage : ToolPage, System.IDisposable
{
    public override string ShortName => "SetupFlow";

    public SetupFlowViewModel ViewModel { get; }

    public SetupFlowPage()
    {
        ViewModel = Application.Current.GetService<SetupFlowViewModel>();
        InitializeComponent();
        this.Loaded += (_, _) =>
        {
            NavigationTeachingTip.XamlRoot = XamlRoot;
        };

        _teachingTipVisabilityTimer.Elapsed += ShowTeachingTip;
        _teachingTipHidingTimer.Elapsed += HideTeachingTip;
    }

    /// <summary>
    /// Timer, that, when elapsed, will show the teaching tip.
    /// </summary>
    private readonly Timer _teachingTipVisabilityTimer = new (1500);

    /// <summary>
    /// Timer, that, when elapsed, will hide the teaching tip.
    /// </summary>
    private readonly Timer _teachingTipHidingTimer = new (200);

    private void ShowTeachingTip(object sender, ElapsedEventArgs args)
    {
        _teachingTipVisabilityTimer.Enabled = false;
        _teachingTipHidingTimer.Enabled = false;
        Application.Current.GetService<WindowEx>().DispatcherQueue.TryEnqueue(() =>
        {
            NavigationTeachingTip.Visibility = Visibility.Visible;
            NavigationTeachingTip.IsOpen = true;
        });
    }

    private void HideTeachingTip(object sender, ElapsedEventArgs args)
    {
        _teachingTipVisabilityTimer.Enabled = false;
        _teachingTipHidingTimer.Enabled = false;
        Application.Current.GetService<WindowEx>().DispatcherQueue.TryEnqueue(() =>
        {
            NavigationTeachingTip.Visibility = Visibility.Collapsed;
            NavigationTeachingTip.IsOpen = false;
        });
    }

    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _teachingTipVisabilityTimer.Enabled = true;
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _teachingTipHidingTimer.Enabled = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _teachingTipVisabilityTimer.Dispose();
    }
}
