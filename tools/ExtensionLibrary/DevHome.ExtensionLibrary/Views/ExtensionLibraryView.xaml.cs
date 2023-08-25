// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Common;
using DevHome.ExtensionLibrary.ViewModels;

namespace DevHome.ExtensionLibrary.Views;

public partial class ExtensionLibraryView : ToolPage
{
    public override string ShortName => "Extensions";

    public ExtensionLibraryViewModel ViewModel { get; }

    public ExtensionLibraryView()
    {
        ViewModel = new ExtensionLibraryViewModel();
        this.InitializeComponent();
    }
}
