// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.ExtensionLibrary.Views;

public sealed partial class ExtensionSettingsPage : DevHomePage
{
    public ExtensionSettingsViewModel ViewModel { get; }

    public ExtensionSettingsPage()
    {
        ViewModel = Application.Current.GetService<ExtensionSettingsViewModel>();
        this.InitializeComponent();
    }
}
