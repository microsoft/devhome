// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class ModifyFeaturesDialog : ContentDialog
{
    public ModifyFeaturesDialogViewModel ViewModel { get; }

    public ModifyFeaturesDialog(IAsyncRelayCommand applyChangedCommand)
    {
        ViewModel = new ModifyFeaturesDialogViewModel(applyChangedCommand);
        this.InitializeComponent();
        this.DataContext = ViewModel;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.HandlePrimaryButton();
    }

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.HandleSecondaryButton();
    }
}
