// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.ExtensionLibrary.Views;

public partial class ExtensionLibraryView : ToolPage
{
    public override string DisplayName =>
        new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader(
            "DevHome.ExtensionLibrary.pri",
            "DevHome.ExtensionLibrary/Resources").GetString("NavigationPane/Content");

    public ExtensionLibraryViewModel ViewModel { get; }

    internal ExtensionLibraryBannerViewModel BannerViewModel { get; }

    public ExtensionLibraryView()
    {
        ViewModel = Application.Current.GetService<ExtensionLibraryViewModel>();
        BannerViewModel = Application.Current.GetService<ExtensionLibraryBannerViewModel>();
        this.InitializeComponent();
    }
}
