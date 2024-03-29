// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.Customization.Views;

public delegate OptimizeDevDriveDialogViewModel OptimizeDevDriveDialogViewModelFactory(string existingCacheLocation, string environmentVariableToBeSet);

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
