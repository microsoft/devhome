// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.ViewModels.DevDriveInsights;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public delegate OptimizeDevDriveDialogViewModel OptimizeDevDriveDialogViewModelFactory(string existingCacheLocation, string environmentVariableToBeSet, string exampleDevDriveLocation);

public sealed partial class OptimizeDevDriveDialog : ContentDialog
{
    public OptimizeDevDriveDialogViewModel ViewModel
    {
        get;
    }

    public OptimizeDevDriveDialog(OptimizeDevDriveDialogViewModel optimizeDevDriveDialogViewModel)
    {
        ViewModel = optimizeDevDriveDialogViewModel;
        this.InitializeComponent();
    }
}
