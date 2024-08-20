// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.ExtensionLibrary.Views;

public sealed partial class ExtensionNavigationPage : ToolPage
{
    public ExtensionNavigationViewModel ViewModel { get; }

    public ExtensionNavigationPage()
    {
        ViewModel = Application.Current.GetService<ExtensionNavigationViewModel>();
        this.InitializeComponent();
    }
}
