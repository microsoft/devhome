// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.Services;
using Microsoft.Management.Deployment;
using Windows.System;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

/// <summary>
/// ViewModel class for the <see cref="Package"/> model.
/// </summary>
public partial class PackageViewModel : ObservableObject
{
    private static readonly Uri DefaultPackageIconSource = new ("ms-appx:///DevHome.SetupFlow/Assets/DefaultPackageIcon.png");
    private readonly IWinGetPackage _package;
    private readonly IWindowsPackageManager _wpm;

    /// <summary>
    /// Occurrs after the package selection changes
    /// </summary>
    public event EventHandler<PackageViewModel> SelectionChanged;

    /// <summary>
    /// Indicates if a package is selected
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    public PackageViewModel(IWindowsPackageManager wpm, IWinGetPackage package)
    {
        _wpm = wpm;
        _package = package;
    }

    public string Name => _package.Name;

    public Uri ImageUri => _package.ImageUri ?? DefaultPackageIconSource;

    public string Version => _package.Version;

    /// <summary>
    /// Gets the learn more button uri.
    /// For packages from winget or custom catalogs:
    /// 1. Use package url
    /// 2. Else, use publisher url
    /// 3. Else, use "https://github.com/microsoft/winget-pkgs"
    ///
    /// For packages from ms store catalog:
    /// 1. Use package url
    /// 2. Else, use "ms-windows-store://pdp?productid={ID}"
    /// </summary>
    /// <returns>Learn more button uri</returns>
    public Uri GetLearnMoreUri()
    {
        if (_package.PackageUrl != null)
        {
            return _package.PackageUrl;
        }

        if (_package.CatalogId == _wpm.MsStoreId)
        {
            return new Uri($"ms-windows-store://pdp/?productid={_package.Id}");
        }

        if (_package.PublisherUrl != null)
        {
            return _package.PublisherUrl;
        }

        return new Uri("https://github.com/microsoft/winget-pkgs");
    }

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(null, this);

    /// <summary>
    /// Toggle package selection
    /// </summary>
    [RelayCommand]
    private void ToggleSelection() => IsSelected = !IsSelected;

    /// <summary>
    /// Command for launching a 'Learn more' uri
    /// </summary>
    [RelayCommand]
    private async Task LearnMoreAsync()
    {
        await Launcher.LaunchUriAsync(GetLearnMoreUri());
    }
}
