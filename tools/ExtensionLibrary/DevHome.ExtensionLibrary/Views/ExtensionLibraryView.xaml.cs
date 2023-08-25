// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.ExtensionLibrary.Views;

public partial class ExtensionLibraryView : ToolPage
{
    public override string ShortName => "Extensions";

    public ExtensionLibraryViewModel ViewModel { get; }

    public ExtensionLibraryView()
    {
        ViewModel = Application.Current.GetService<ExtensionLibraryViewModel>();
        this.InitializeComponent();
    }
}
