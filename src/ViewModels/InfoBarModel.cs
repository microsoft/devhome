// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
