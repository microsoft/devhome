// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using WinUIEx;

namespace DevHome.Common.Windows;

[INotifyPropertyChanged]
public sealed partial class Example2 : SecondaryWindow
{
    [ObservableProperty]
    private string _packageTitle;

    public Example2()
    {
        this.InitializeComponent();

        PackageTitle = "Example2";
    }
}
