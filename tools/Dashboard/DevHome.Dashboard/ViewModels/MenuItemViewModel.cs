// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Dashboard.ViewModels;

internal sealed partial class MenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    private BitmapImage _image;

    [ObservableProperty]
    private string _text;

    // WidgetProviderDefinition or WidgetDefinition
    [ObservableProperty]
    private object _tag;

    [ObservableProperty]
    private bool _isEnabled;

    public ObservableCollection<MenuItemViewModel> SubMenuItems { get; set; } = [];
}
