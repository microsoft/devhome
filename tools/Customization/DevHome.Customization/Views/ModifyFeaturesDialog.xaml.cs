// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class ModifyFeaturesDialog : ContentDialog
{
    public ModifyFeaturesDialogViewModel ViewModel { get; }

    public ModifyFeaturesDialog()
    {
        ViewModel = new ModifyFeaturesDialogViewModel();
        this.InitializeComponent();
        this.DataContext = ViewModel;
    }

    private void OnCancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.HandleCancel();
    }
}
