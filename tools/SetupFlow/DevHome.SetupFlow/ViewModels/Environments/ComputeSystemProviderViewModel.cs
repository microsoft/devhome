// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.ViewModels.Environments;

public class ComputeSystemProviderViewModel
{
    private readonly string _packageFullName;

    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public string DisplayName => ProviderDetails.ComputeSystemProvider.DisplayName;

    public ImageIcon Icon { get; set; }

    public ComputeSystemProviderViewModel(ComputeSystemProviderDetails providerDetails)
    {
        ProviderDetails = providerDetails;
        _packageFullName = ProviderDetails.ExtensionWrapper.PackageFullName;
        Icon = GetImageIcon();
    }

    private ImageIcon GetImageIcon()
    {
        var imageIcon = new ImageIcon();
        imageIcon.Source = CardProperty.ConvertMsResourceToIcon(ProviderDetails.ComputeSystemProvider.Icon, _packageFullName);
        return imageIcon;
    }
}
