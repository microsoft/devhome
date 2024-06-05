// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.ExtensionLibrary.Views;

public sealed partial class ExtensionSettingsPage : Page
{
    public ExtensionSettingsViewModel ViewModel { get; }

    public ExtensionSettingsPage()
    {
        ViewModel = Application.Current.GetService<ExtensionSettingsViewModel>();
        this.InitializeComponent();
    }
}
