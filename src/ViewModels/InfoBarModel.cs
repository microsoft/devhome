// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.ViewModels;

public partial class InfoBarModel : ObservableObject
{
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private bool _isOpen = false;
}
