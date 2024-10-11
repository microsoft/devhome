// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Controls;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.ExtensionLibrary.Views;

public partial class ExtensionLibraryView : ToolPage
{
    public ExtensionLibraryViewModel ViewModel { get; }

    internal ExtensionLibraryBannerViewModel BannerViewModel { get; }

    public ContentAlignment AlignmentForContent { get; } = ContentAlignment.Left;

    public ExtensionLibraryView()
    {
        ViewModel = Application.Current.GetService<ExtensionLibraryViewModel>();
        BannerViewModel = Application.Current.GetService<ExtensionLibraryBannerViewModel>();
        this.InitializeComponent();
    }
}
