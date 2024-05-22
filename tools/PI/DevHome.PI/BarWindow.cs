// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32.Foundation;
using WinUIEx;

namespace DevHome.PI;

public partial class BarWindow
{
    private readonly Settings settings = Settings.Default;
    private readonly BarWindowHorizontal horizontalWindow;
    private readonly BarWindowVertical verticalWindow;
    private readonly BarWindowViewModel viewModel = new();

    internal HWND ThisHwnd
    {
        get
        {
            if (horizontalWindow.Visible)
            {
                return horizontalWindow.ThisHwnd;
            }
            else
            {
                return verticalWindow.ThisHwnd;
            }
        }
    }

    public List<Window> OpenChildWindows { get; private set; } = [];

    public void Close()
    {
        horizontalWindow.Close();
        verticalWindow.Close();
    }

    public Frame GetFrame() => horizontalWindow.GetFrame();

    public IntPtr GetWindowHandle()
    {
        if (horizontalWindow.Visible)
        {
            return horizontalWindow.GetWindowHandle();
        }
        else
        {
            return verticalWindow.GetWindowHandle();
        }
    }

    internal void SetRequestedTheme(ElementTheme theme)
    {
        horizontalWindow.SetRequestedTheme(theme);
        verticalWindow.SetRequestedTheme(theme);
    }

    public void NavigateTo(Type viewModelType) => horizontalWindow.NavigateTo(viewModelType);

    public BarWindow()
    {
        horizontalWindow = new BarWindowHorizontal(viewModel);
        verticalWindow = new BarWindowVertical(viewModel);

        horizontalWindow.Closed += Window_Closed;
        verticalWindow.Closed += Window_Closed;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        settings.PropertyChanged += Settings_PropertyChanged;

        if (settings.IsCpuUsageMonitoringEnabled)
        {
            PerfCounters.Instance.Start();
        }

        horizontalWindow.Show();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsCpuUsageMonitoringEnabled))
        {
            if (settings.IsCpuUsageMonitoringEnabled)
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
        if (e.PropertyName == nameof(BarWindowViewModel.BarOrientation))
        {
            RotateBar();
        }
    }

    public void RotateBar()
    {
        if (horizontalWindow.Visible)
        {
            horizontalWindow.Hide();
            verticalWindow.Show();
        }
        else
        {
            verticalWindow.Hide();
            horizontalWindow.Show();
        }
    }
}
