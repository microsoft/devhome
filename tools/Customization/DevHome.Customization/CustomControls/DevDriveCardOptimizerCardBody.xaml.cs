// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using DevHome.Customization.Views;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.Customization.CustomControls;

public sealed partial class DevDriveOptimizerCardBody : UserControl
{
    public DevDriveOptimizerCardBody()
    {
        this.InitializeComponent();
    }

    public DataTemplate ActionControlTemplate
    {
        get => (DataTemplate)GetValue(ActionControlTemplateProperty);
        set => SetValue(ActionControlTemplateProperty, value);
    }

    public string CacheToBeMoved
    {
        get => (string)GetValue(CacheToBeMovedProperty);
        set => SetValue(CacheToBeMovedProperty, value);
    }

    public string CacheLocation
    {
        get => (string)GetValue(CacheLocationProperty);
        set => SetValue(CacheLocationProperty, value);
    }

    public string OptimizationDescription
    {
        get => (string)GetValue(OptimizationDescriptionProperty);
        set => SetValue(OptimizationDescriptionProperty, value);
    }

    public OptimizeDevDriveDialog OptimizeDevDriveDialog
    {
        get => (OptimizeDevDriveDialog)GetValue(OptimizeDevDriveDialogProperty);
        set => SetValue(OptimizeDevDriveDialogProperty, value);
    }

    /// <summary>
    /// User wants to optimize a dev drive.
    /// </summary>
    [RelayCommand]
    private async Task OptimizeDevDriveAsync()
    {
        OptimizeDevDriveDialog = new OptimizeDevDriveDialog();
        OptimizeDevDriveDialog.XamlRoot = this.Content.XamlRoot;
        OptimizeDevDriveDialog.RequestedTheme = ActualTheme;

        await OptimizeDevDriveDialog.ShowAsync();
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty CacheToBeMovedProperty = DependencyProperty.Register(nameof(CacheToBeMoved), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty CacheLocationProperty = DependencyProperty.Register(nameof(CacheLocation), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty OptimizationDescriptionProperty = DependencyProperty.Register(nameof(OptimizationDescription), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty OptimizeDevDriveDialogProperty = DependencyProperty.Register(nameof(OptimizeDevDriveDialog), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
}
