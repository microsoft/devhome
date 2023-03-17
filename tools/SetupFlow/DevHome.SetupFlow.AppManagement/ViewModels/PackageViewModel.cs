// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.AppManagement.Models;

namespace DevHome.SetupFlow.AppManagement.ViewModels;

/// <summary>
/// ViewModel class for the <see cref="Package"/> model.
/// </summary>
public partial class PackageViewModel : ObservableObject
{
    private static readonly Uri DefaultPackageIconSource = new ("ms-appx:///DevHome.SetupFlow/Assets/DefaultPackageIcon.png");
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
    }

    public (string, string) CompositeKey => _package.CompositeKey;

    public IWinGetPackage Package => _package;

    public string Name => _package.Name;

    public Uri ImageUri => _package.ImageUri ?? DefaultPackageIconSource;

    public string Version => _package.Version;

    public Uri PackageUri => _package.PackageUri;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(null, this);

    /// <summary>
    /// Toggle package selection
    /// </summary>
    [RelayCommand]
    private void ToggleSelection() => IsSelected = !IsSelected;
}
