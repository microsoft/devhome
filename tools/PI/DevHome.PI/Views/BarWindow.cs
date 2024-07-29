// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Win32.Foundation;
using WinUIEx;

namespace DevHome.PI.Views;

public partial class BarWindow
{
    private readonly string _aliasDisabledDialogTitle = CommonHelper.GetLocalizedString("AliasDisabledDialogTitle");
    private readonly string _aliasDisabledDialogContent = CommonHelper.GetLocalizedString("AliasDisabledDialogContent");
    private readonly string _aliasDisabledDialogButtonText = CommonHelper.GetLocalizedString("AliasDisabledDialogButtonText");

    private readonly Settings _settings = Settings.Default;
    private readonly BarWindowHorizontal _horizontalWindow;
    private readonly BarWindowVertical _verticalWindow;
    private readonly BarWindowViewModel _viewModel = Application.Current.GetService<BarWindowViewModel>();

    internal HWND CurrentHwnd
    {
        get
        {
            if (_viewModel.BarOrientation == Orientation.Horizontal)
            {
                return _horizontalWindow.ThisHwnd;
            }
            else
            {
                return _verticalWindow.ThisHwnd;
            }
        }
    }

    public List<Window> OpenChildWindows { get; private set; } = [];

    public void Close()
    {
        UnsnapBarWindow();

        _horizontalWindow.Close();
        _verticalWindow.Close();

        foreach (var window in OpenChildWindows)
        {
            window.Close();
        }
    }

    public Frame GetFrame() => _horizontalWindow.GetFrame();

    internal void SetRequestedTheme(ElementTheme theme)
    {
        _horizontalWindow.SetRequestedTheme(theme);
        _verticalWindow.SetRequestedTheme(theme);
    }

    public void NavigateTo(Type viewModelType) => _horizontalWindow.NavigateTo(viewModelType);

    public void NavigateToPiSettings(string settingsPage) => _horizontalWindow.NavigateToPiSettings(settingsPage);

    public BarWindow()
    {
        _horizontalWindow = new BarWindowHorizontal(_viewModel);
        _verticalWindow = new BarWindowVertical(_viewModel);

        _horizontalWindow.Closed += Window_Closed;
        _verticalWindow.Closed += Window_Closed;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _settings.PropertyChanged += Settings_PropertyChanged;

        if (_settings.IsCpuUsageMonitoringEnabled)
        {
            PerfCounters.Instance.Start();
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // If we receive a window closed event, clean up the system
        TargetAppData.Instance.ClearAppData();
        PerfCounters.Instance.Stop();

        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        primaryWindow.ClearBarWindow();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(Settings.IsCpuUsageMonitoringEnabled), StringComparison.Ordinal))
        {
            if (_settings.IsCpuUsageMonitoringEnabled)
            {
                PerfCounters.Instance.Start();
            }
            else
            {
                PerfCounters.Instance.Stop();
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(BarWindowViewModel.BarOrientation), StringComparison.Ordinal))
        {
            RotateBar();
        }
    }

    public void ShowDialogToEnableAppExecutionAlias()
    {
        _ = _horizontalWindow.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = _aliasDisabledDialogTitle;
            dialog.Content = new TextBlock()
            {
                Text = _aliasDisabledDialogContent,
                TextWrapping = TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = _aliasDisabledDialogButtonText;
            dialog.PrimaryButtonCommand = _viewModel.LaunchAdvancedAppsPageInWindowsSettingsCommand;
        });
    }

    public void RotateBar()
    {
        if (_horizontalWindow.Visible)
        {
            _verticalWindow.UpdatePositionFromHwnd(CurrentHwnd);

            _horizontalWindow.Hide();
            _verticalWindow.Show();
        }
        else
        {
            _horizontalWindow.UpdatePositionFromHwnd(CurrentHwnd);
            _verticalWindow.Hide();
            _horizontalWindow.Show();
        }
    }

    public void UpdateBarWindowPosition(PointInt32 position)
    {
        _viewModel.WindowPosition = position;
    }

    public void UpdateBarWindowSize(SizeInt32 size)
    {
        _viewModel.RequestedWindowSize = size;
    }

    public void ResetBarWindowOnTop()
    {
        _viewModel.ResetBarWindowOnTop();
    }

    public void UnsnapBarWindow()
    {
        _viewModel.UnsnapBarWindow();
    }

    public bool IsBarSnappedToWindow()
    {
        return _viewModel.IsSnapped;
    }
}
