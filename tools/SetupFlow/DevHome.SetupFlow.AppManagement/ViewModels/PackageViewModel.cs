// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
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

    public PackageViewModel(IWinGetPackage package, IWindowsPackageManager wpm)
    {
        _package = package;
        _wpm = wpm;
    }

    public string Name => _package.Name;

    public Uri ImageUri => _package.ImageUri ?? DefaultPackageIconSource;

    public string Version => _package.Version;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(null, this);

    /// <summary>
    /// Toggle package selection
    /// </summary>
    [RelayCommand]
    private void ToggleSelection() => IsSelected = !IsSelected;

    [RelayCommand]
    private async void LearnMore()
    {
        await Launcher.LaunchUriAsync(GetLearnMoreUri());
    }

    /// <summary>
    /// Gets the learn more button uri.
    /// For packages from winget or private catalogs:
    /// 1. Use package url
    /// 2. Use publisher url
    /// 3. Use "https://github.com/microsoft/winget-pkgs"
    ///
    /// For packages from ms store catalog:
    /// 1. Use package url
    /// 2. Use "ms-windows-store://pdp?ProductId={ID}"
    /// </summary>
    /// <returns>Learn more button uri</returns>
    private Uri GetLearnMoreUri()
    {
        var packageUrl = _package.PackageUrl;
        if (packageUrl != null)
        {
            return packageUrl;
        }

        if (_wpm.IsPackageFromCatalog(_package, PredefinedPackageCatalog.MicrosoftStore))
        {
            return new Uri($"ms-windows-store://pdp/?productid={_package.Id}");
        }

        var publisherUrl = _package.PublisherUrl;
        if (publisherUrl != null)
        {
            return publisherUrl;
        }

        return new Uri("https://github.com/microsoft/winget-pkgs");
    }
}
