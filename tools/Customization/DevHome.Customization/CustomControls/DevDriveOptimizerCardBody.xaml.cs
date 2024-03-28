// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Customization.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    public string DevDriveOptimizationSuggestion
    {
        get => (string)GetValue(DevDriveOptimizationSuggestionProperty);
        set => SetValue(DevDriveOptimizationSuggestionProperty, value);
    }

    public string ExistingCacheLocation
    {
        get => (string)GetValue(ExistingCacheLocationProperty);
        set => SetValue(ExistingCacheLocationProperty, value);
    }

    public string ExampleLocationOnDevDrive
    {
        get => (string)GetValue(ExampleLocationOnDevDriveProperty);
        set => SetValue(ExampleLocationOnDevDriveProperty, value);
    }

    public string EnvironmentVariableToBeSet
    {
        get => (string)GetValue(EnvironmentVariableToBeSetProperty);
        set => SetValue(EnvironmentVariableToBeSetProperty, value);
    }

    public string OptimizerDevDriveDescription
    {
        get => (string)GetValue(OptimizerDevDriveDescriptionProperty);
        set => SetValue(OptimizerDevDriveDescriptionProperty, value);
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
        OptimizeDevDriveDialog = new OptimizeDevDriveDialog(CacheToBeMoved, ExistingCacheLocation, EnvironmentVariableToBeSet);
        OptimizeDevDriveDialog.XamlRoot = this.Content.XamlRoot;
        OptimizeDevDriveDialog.RequestedTheme = ActualTheme;

        await OptimizeDevDriveDialog.ShowAsync();
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty CacheToBeMovedProperty = DependencyProperty.Register(nameof(CacheToBeMoved), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveOptimizationSuggestionProperty = DependencyProperty.Register(nameof(DevDriveOptimizationSuggestion), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ExistingCacheLocationProperty = DependencyProperty.Register(nameof(ExistingCacheLocation), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ExampleLocationOnDevDriveProperty = DependencyProperty.Register(nameof(ExampleLocationOnDevDrive), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty EnvironmentVariableToBeSetProperty = DependencyProperty.Register(nameof(EnvironmentVariableToBeSet), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty OptimizerDevDriveDescriptionProperty = DependencyProperty.Register(nameof(OptimizerDevDriveDescription), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty OptimizeDevDriveDialogProperty = DependencyProperty.Register(nameof(OptimizeDevDriveDialog), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
}
