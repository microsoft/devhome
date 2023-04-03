// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class ExtensionsPage : Page
{
    public ExtensionsViewModel ViewModel
    {
        get;
    }

    public ExtensionsPage()
    {
        ViewModel = new ExtensionsViewModel();
        this.InitializeComponent();
    }
}
