// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.ViewModels;

public partial class VirtualMachineManagementViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcherQueue;

    private readonly StringResource _commonStringResource;

    private StackedNotificationsBehavior? _notificationsHelper;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    public bool FeaturesLoaded => !LoadFeaturesCommand.IsRunning;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public bool ChangesApplied => !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<OptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => FeaturesLoaded && Features.Any(f => f.HasChanged);

    public VirtualMachineManagementViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("VirtualMachineManagement_Header"), typeof(VirtualMachineManagementViewModel).FullName!)
        ];

        _commonStringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");

        LoadFeaturesCommand = new AsyncRelayCommand(LoadFeaturesAsync);
        LoadFeaturesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoadFeaturesCommand.IsRunning))
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    OnPropertyChanged(nameof(FeaturesLoaded));
                    OnPropertyChanged(nameof(HasFeatureChanges));
                });
            }
        };

        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        ApplyChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ApplyChangesCommand.IsRunning))
            {
                await _dispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(ChangesApplied)));
            }
        };

        _ = LoadFeaturesCommand.ExecuteAsync(null);
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = notificationQueue;
    }

    private void FeatureState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptionalFeatureState.IsEnabled))
        {
            OnPropertyChanged(nameof(HasFeatureChanges));
        }
    }

    private async Task LoadFeaturesAsync()
    {
        await Task.Run(async () =>
        {
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                Features.Clear();
            });

            foreach (var featureName in WindowsOptionalFeatureNames.VirtualMachineFeatures)
            {
                var feature = ManagementInfrastructureHelper.GetWindowsFeatureDetails(featureName);
                if (feature != null)
                {
                    var featureState = new OptionalFeatureState(feature);
                    featureState.PropertyChanged += FeatureState_PropertyChanged;

                    await _dispatcherQueue.EnqueueAsync(() =>
                    {
                        Features.Add(featureState);
                    });
                }
            }
        });
    }

    private async Task ApplyChangesAsync()
    {
        await Task.Run(async () =>
        {
            await _dispatcherQueue.EnqueueAsync(async () =>
            {
                await LoadFeaturesCommand.ExecuteAsync(null);
            });

            // TODO: Use script to apply changes and prompt to restart the computer. Keep track of whether or not a reboot is required
            // and disable all the toggles until the reboot is complete.
            _notificationsHelper?.ShowWithWindowExtension(
                "Changes applied",
                _commonStringResource.GetLocalized("RestartAfterChangesMessage"),
                InfoBarSeverity.Warning,
                RestartComputerCommand,
                _commonStringResource.GetLocalized("RestartButton"));
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private void RestartComputer()
    {
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,

            // Restart the computer
            FileName = Environment.SystemDirectory + "\\shutdown.exe",
            Arguments = "-r -t 0",
            Verb = string.Empty,
        };

        var process = new Process
        {
            StartInfo = startInfo,
        };
        process.Start();
    }
}
