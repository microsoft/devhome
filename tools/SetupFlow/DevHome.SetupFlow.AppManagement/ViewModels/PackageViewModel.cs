// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.AppManagement.Models;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

/// <summary>
/// ViewModel class for the <see cref="Package"/> model.
/// </summary>
public partial class PackageViewModel : ObservableObject
{
    private static readonly BitmapImage DefaultLightPackageIconSource = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/DefaultLightPackageIcon.png"));
    private static readonly BitmapImage DefaultDarkPackageIconSource = new (new Uri("ms-appx:///DevHome.SetupFlow/Assets/DefaultDarkPackageIcon.png"));
    private readonly Lazy<BitmapImage> _packageDarkThemeIcon;
    private readonly Lazy<BitmapImage> _packageLightThemeIcon;
    private readonly IWinGetPackage _package;

    /// <summary>
    /// Occurrs after the package selection changes
    /// </summary>
    public event EventHandler<PackageViewModel> SelectionChanged;

    /// <summary>
    /// Indicates if a package is selected
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    public PackageViewModel(IWinGetPackage package)
    {
        _package = package;
        _packageDarkThemeIcon = new Lazy<BitmapImage>(() => GetIconByTheme(RestoreApplicationIconTheme.Dark));
        _packageLightThemeIcon = new Lazy<BitmapImage>(() => GetIconByTheme(RestoreApplicationIconTheme.Light));
    }

    public string Name => _package.Name;

    public BitmapImage Icon => GetIcon();

    public string Version => _package.Version;

    public Uri PackageUri => _package.PackageUri;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(null, this);

    /// <summary>
    /// Toggle package selection
    /// </summary>
    [RelayCommand]
    private void ToggleSelection() => IsSelected = !IsSelected;

    /// <summary>
    /// Gets the package icon based on the provided theme
    /// </summary>
    /// <param name="theme">Package icon theme</param>
    /// <returns>Package icon</returns>
    private BitmapImage GetIconByTheme(RestoreApplicationIconTheme theme)
    {
        if (theme == RestoreApplicationIconTheme.Dark)
        {
            // Get default dark theme icon if corresponding package icon was not found
            if (_package.DarkThemeIcon == null)
            {
                return DefaultDarkPackageIconSource;
            }

            var darkThemeBitmapImage = new BitmapImage();
            darkThemeBitmapImage.SetSource(_package.DarkThemeIcon);
            return darkThemeBitmapImage;
        }

        // Get default light theme icon if corresponding package icon was not found
        if (_package.LightThemeIcon == null)
        {
            return DefaultLightPackageIconSource;
        }

        var lightThemeBitmapImage = new BitmapImage();
        lightThemeBitmapImage.SetSource(_package.LightThemeIcon);
        return lightThemeBitmapImage;
    }

    /// <summary>
    /// Gets the package icon based on the application theme
    /// </summary>
    /// <returns>Package icon</returns>
    private BitmapImage GetIcon()
    {
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        return themeService.IsDarkTheme() ? _packageDarkThemeIcon.Value : _packageLightThemeIcon.Value;
    }
}
