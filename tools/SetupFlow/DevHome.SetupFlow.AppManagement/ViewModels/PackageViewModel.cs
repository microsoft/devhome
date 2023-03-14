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

    public BitmapImage LightThemeIcon => _packageLightThemeIcon.Value;

    public BitmapImage DarkThemeIcon => _packageDarkThemeIcon.Value;

    public BitmapImage Icon => GetIcon();

    public string Version => _package.Version;

    public Uri PackageUri => _package.PackageUri;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(null, this);

    /// <summary>
    /// Toggle package selection
    /// </summary>
    [RelayCommand]
    private void ToggleSelection() => IsSelected = !IsSelected;

    private BitmapImage GetIconByTheme(RestoreApplicationIconTheme theme)
    {
        // Package dark theme icon
        if (theme == RestoreApplicationIconTheme.Dark && _package.DarkThemeIcon != null)
        {
            var image = new BitmapImage();
            image.SetSource(_package.DarkThemeIcon);
            return image;
        }

        // Package light theme icon
        if (theme == RestoreApplicationIconTheme.Light && _package.LightThemeIcon != null)
        {
            var image = new BitmapImage();
            image.SetSource(_package.LightThemeIcon);
            return image;
        }

        return theme == RestoreApplicationIconTheme.Dark ? DefaultDarkPackageIconSource : DefaultLightPackageIconSource;
    }

    private BitmapImage GetIcon()
    {
        var theme = Application.Current.GetService<IThemeSelectorService>();
        if (theme.Theme == ElementTheme.Dark)
        {
            return DarkThemeIcon;
        }

        if (theme.Theme == ElementTheme.Light)
        {
            return LightThemeIcon;
        }

        // Default
        var applicationTheme = Application.Current.RequestedTheme;
        if (applicationTheme == ApplicationTheme.Dark)
        {
            return DarkThemeIcon;
        }

        return LightThemeIcon;
    }
}
