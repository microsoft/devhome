// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.ViewModels.Environments;

public partial class ComputeSystemProviderViewModel : ObservableObject
{
    private readonly string _packageFullName;

    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private ImageIcon _icon;

    [ObservableProperty]
    private bool _isSelected;

    public ComputeSystemProviderViewModel(ComputeSystemProviderDetails providerDetails)
    {
        ProviderDetails = providerDetails;
        _packageFullName = ProviderDetails.ExtensionWrapper.PackageFullName;
        Icon = GetImageIcon();
        DisplayName = ProviderDetails.ComputeSystemProvider.DisplayName;
    }

    private ImageIcon GetImageIcon()
    {
        var imageIcon = new ImageIcon();
        imageIcon.Source = CardProperty.ConvertMsResourceToIcon(ProviderDetails.ComputeSystemProvider.Icon, _packageFullName);
        return imageIcon;
    }
}
