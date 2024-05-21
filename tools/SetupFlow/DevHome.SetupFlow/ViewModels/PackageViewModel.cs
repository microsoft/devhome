// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Delegate factory for creating package view models
/// </summary>
/// <param name="package">WinGet package</param>
/// <returns>Package view model</returns>
public delegate PackageViewModel PackageViewModelFactory(IWinGetPackage package);

/// <summary>
/// ViewModel class for the <see cref="Package"/> model.
/// </summary>
public partial class PackageViewModel : ObservableObject
{
    private const string PublisherNameNotAvailable = "-";

    private static readonly BitmapImage DefaultLightPackageIconSource = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DefaultLightPackageIcon.png"));
    private static readonly BitmapImage DefaultDarkPackageIconSource = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/DefaultDarkPackageIcon.png"));

    private readonly Lazy<BitmapImage> _packageDarkThemeIcon;
    private readonly Lazy<BitmapImage> _packageLightThemeIcon;

    private readonly ISetupFlowStringResource _stringResource;
    private readonly IWinGetPackage _package;
    private readonly IWindowsPackageManager _wpm;
    private readonly IThemeSelectorService _themeSelector;
    private readonly IScreenReaderService _screenReaderService;
    private readonly SetupFlowOrchestrator _orchestrator;

    /// <summary>
    /// Occurs after the package selection changes
    /// </summary>
    public event EventHandler<bool> SelectionChanged;

    /// <summary>
    /// Occurs after the package version has changed
    /// </summary>
    public event EventHandler<string> VersionChanged;

    /// <summary>
    /// Indicates if a package is selected
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonAutomationName))]
    [NotifyPropertyChangedFor(nameof(ButtonAutomationId))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TooltipVersion))]
    [NotifyPropertyChangedFor(nameof(PackageFullDescription))]
    private string _selectedVersion;

    public bool ShowVersionList => IsVersioningSupported();

    public PackageViewModel(
        ISetupFlowStringResource stringResource,
        IWindowsPackageManager wpm,
        IWinGetPackage package,
        IThemeSelectorService themeSelector,
        IScreenReaderService screenReaderService,
        SetupFlowOrchestrator orchestrator)
    {
        _stringResource = stringResource;
        _wpm = wpm;
        _package = package;
        _themeSelector = themeSelector;
        _screenReaderService = screenReaderService;
        _orchestrator = orchestrator;

        // Lazy-initialize optional or expensive view model members
        _packageDarkThemeIcon = new Lazy<BitmapImage>(() => GetIconByTheme(RestoreApplicationIconTheme.Dark));
        _packageLightThemeIcon = new Lazy<BitmapImage>(() => GetIconByTheme(RestoreApplicationIconTheme.Light));

        SelectedVersion = GetDefaultSelectedVersion();
        InstallPackageTask = CreateInstallTask();
    }

    public PackageUniqueKey UniqueKey => _package.UniqueKey;

    public IWinGetPackage Package => _package;

    public BitmapImage Icon => _themeSelector.IsDarkTheme() ? _packageDarkThemeIcon.Value : _packageLightThemeIcon.Value;

    public string Name => _package.Name;

    public string InstalledVersion => _package.InstalledVersion;

    public IReadOnlyList<string> AvailableVersions => _package.AvailableVersions;

    public bool IsInstalled => _package.IsInstalled;

    public string CatalogName => _package.CatalogName;

    public string PublisherName => string.IsNullOrWhiteSpace(_package.PublisherName) ? PublisherNameNotAvailable : _package.PublisherName;

    public string InstallationNotes => _package.InstallationNotes;

    public string PackageFullDescription => GetPackageFullDescription();

    public string PackageShortDescription => GetPackageShortDescription();

    public string PackageTitle => Name;

    public string TooltipName => _stringResource.GetLocalized(StringResourceKey.PackageNameTooltip, Name);

    public string TooltipVersion => _stringResource.GetLocalized(StringResourceKey.PackageVersionTooltip, SelectedVersion);

    public string TooltipIsInstalled => IsInstalled ? _stringResource.GetLocalized(StringResourceKey.PackageInstalledTooltip) : string.Empty;

    public string TooltipSource => _stringResource.GetLocalized(StringResourceKey.PackageSourceTooltip, CatalogName);

    public string TooltipPublisher => _stringResource.GetLocalized(StringResourceKey.PackagePublisherNameTooltip, PublisherName);

    public bool CanInstall => _orchestrator.IsSettingUpATargetMachine || !IsInstalled || _package.InstalledVersion != SelectedVersion;

    public string ButtonAutomationName => IsSelected ?
        _stringResource.GetLocalized(StringResourceKey.RemoveApplication) :
        _stringResource.GetLocalized(StringResourceKey.AddApplication);

    public string ButtonAutomationId => IsSelected ?
        $"Remove_{_package.Id}" :
        $"Add_{_package.Id}";

    public InstallPackageTask InstallPackageTask { get; private set; }

    /// <summary>
    /// Gets the URI for the "Learn more" button
    /// </summary>
    /// <remarks>
    /// For packages from winget or custom catalogs:
    /// 1. Use package url
    /// 2. Else, use publisher url
    /// 3. Else, use "https://github.com/microsoft/winget-pkgs"
    ///
    /// For packages from ms store catalog:
    /// 1. Use package url
    /// 2. Else, use "ms-windows-store://pdp?productid={ID}"
    /// </remarks>
    /// <returns>Learn more button uri</returns>
    public Uri GetLearnMoreUri()
    {
        if (_package.PackageUrl != null)
        {
            return _package.PackageUrl;
        }

        if (_wpm.IsMsStorePackage(_package))
        {
            return new Uri($"ms-windows-store://pdp/?productid={_package.Id}");
        }

        if (_package.PublisherUrl != null)
        {
            return _package.PublisherUrl;
        }

        return new Uri("https://github.com/microsoft/winget-pkgs");
    }

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(this, value);

    partial void OnSelectedVersionChanged(string value)
    {
        // Update the install task with the new selected version
        InstallPackageTask = CreateInstallTask();

        VersionChanged?.Invoke(this, SelectedVersion);
    }

    /// <summary>
    /// Toggle package selection
    /// </summary>
    [RelayCommand]
    private void ToggleSelection()
    {
        // TODO Explore option to augment a Button with the option to announce a text when invoked.
        // https://github.com/microsoft/devhome/issues/1451
        var announcementText = IsSelected ?
            _stringResource.GetLocalized(StringResourceKey.RemovedApplication, PackageTitle) :
            _stringResource.GetLocalized(StringResourceKey.AddedApplication, PackageTitle);

        IsSelected = !IsSelected;
        _screenReaderService.Announce(announcementText);
    }

    /// <summary>
    /// Gets the package icon based on the provided theme
    /// </summary>
    /// <param name="theme">Package icon theme</param>
    /// <returns>Package icon</returns>
    private BitmapImage GetIconByTheme(RestoreApplicationIconTheme theme)
    {
        return theme switch
        {
            // Get default dark theme icon if corresponding package icon was not found
            RestoreApplicationIconTheme.Dark =>
                _package.DarkThemeIcon == null ? DefaultDarkPackageIconSource : CreateBitmapImage(_package.DarkThemeIcon),

            // Get default light theme icon if corresponding package icon was not found
            _ => _package.LightThemeIcon == null ? DefaultLightPackageIconSource : CreateBitmapImage(_package.LightThemeIcon),
        };
    }

    private BitmapImage CreateBitmapImage(IRandomAccessStream stream)
    {
        var bitmapImage = new BitmapImage();
        stream.Seek(0);
        bitmapImage.SetSource(stream);
        return bitmapImage;
    }

    private InstallPackageTask CreateInstallTask()
    {
        return _package.CreateInstallTask(_wpm, _stringResource, SelectedVersion, _orchestrator.ActivityId);
    }

    private string GetPackageShortDescription()
    {
        // Source | Publisher name
        if (!string.IsNullOrEmpty(_package.PublisherName))
        {
            return _stringResource.GetLocalized(StringResourceKey.PackageDescriptionTwoParts, CatalogName, PublisherName);
        }

        // Source
        return CatalogName;
    }

    private string GetPackageFullDescription()
    {
        // Version | Source | Publisher name
        if (!_wpm.IsMsStorePackage(_package) && !string.IsNullOrEmpty(_package.PublisherName))
        {
            return _stringResource.GetLocalized(StringResourceKey.PackageDescriptionThreeParts, SelectedVersion, CatalogName, PublisherName);
        }

        // Version | Source
        if (!_wpm.IsMsStorePackage(_package))
        {
            return _stringResource.GetLocalized(StringResourceKey.PackageDescriptionTwoParts, SelectedVersion, CatalogName);
        }

        // Source | Publisher name
        if (!string.IsNullOrEmpty(_package.PublisherName))
        {
            return _stringResource.GetLocalized(StringResourceKey.PackageDescriptionTwoParts, CatalogName, PublisherName);
        }

        // Source
        return CatalogName;
    }

    /// <summary>
    /// Indicates if a specific version of the package can be selected to install
    /// </summary>
    /// <returns>True a package version can be specified to install</returns>
    private bool IsVersioningSupported()
    {
        // Store packages have a single version
        return !_wpm.IsMsStorePackage(_package);
    }

    /// <summary>
    /// Get the default selected version
    /// </summary>
    /// <returns>Default selected version</returns>
    private string GetDefaultSelectedVersion()
    {
        return _package.IsInstalled ? _package.InstalledVersion : _package.DefaultInstallVersion;
    }

    /// <summary>
    /// Command for launching a 'Learn more' uri
    /// </summary>
    [RelayCommand]
    private async Task LearnMoreAsync()
    {
        await Launcher.LaunchUriAsync(GetLearnMoreUri());
    }
}
