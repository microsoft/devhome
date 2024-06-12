// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class VirtualMachineManagementViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VirtualMachineManagementViewModel));

    private readonly bool _isUserAdministrator = WindowsIdentityHelper.IsUserAdministrator();

    private readonly Window _window;

    private OptionalFeatureNotificationHelper? _notificationsHelper;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    public bool FeaturesLoaded => !LoadFeaturesCommand.IsRunning;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public bool ChangesCanBeApplied => HasFeatureChanges && !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<OptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => _isUserAdministrator && FeaturesLoaded && Features.Any(f => f.HasChanged);

    public VirtualMachineManagementViewModel(Window window)
    {
        _window = window;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("VirtualMachineManagement_Header"), typeof(VirtualMachineManagementViewModel).FullName!)
        ];

        LoadFeaturesCommand = new AsyncRelayCommand(LoadFeaturesAsync);
        LoadFeaturesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoadFeaturesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        ApplyChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ApplyChangesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        _ = LoadFeaturesCommand.ExecuteAsync(null);
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = new(_window, notificationQueue, _log);

        if (!_isUserAdministrator)
        {
            _window.DispatcherQueue.EnqueueAsync(_notificationsHelper.ShowNonAdminUserNotification);
        }
    }

    private async void FeatureState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptionalFeatureState.IsEnabled))
        {
            await OnFeaturesChanged();
        }
    }

    private async Task LoadFeaturesAsync()
    {
        await Task.Run(async () =>
        {
            await _window.DispatcherQueue.EnqueueAsync(() =>
            {
                Features.Clear();
            });

            foreach (var featureName in WindowsOptionalFeatureNames.VirtualMachineFeatures)
            {
                var feature = ManagementInfrastructureHelper.GetWindowsFeatureDetails(featureName);
                if (feature != null && feature.IsAvailable)
                {
                    var featureState = new OptionalFeatureState(feature, _isUserAdministrator, ApplyChangesCommand);
                    featureState.PropertyChanged += FeatureState_PropertyChanged;

                    await _window.DispatcherQueue.EnqueueAsync(() =>
                    {
                        Features.Add(featureState);
                    });
                }
            }
        });
    }

    private async Task ApplyChangesAsync()
    {
        await _window.DispatcherQueue.EnqueueAsync(async () =>
        {
            await ModifyWindowsOptionalFeatures.ModifyFeaturesAsync(Features, _notificationsHelper, _log);
            await LoadFeaturesCommand.ExecuteAsync(null);
        });
    }

    [RelayCommand]
    private void ResetChanges()
    {
        foreach (var feature in Features)
        {
            feature.IsEnabled = feature.Feature.IsEnabled;
        }
    }

    private async Task OnFeaturesChanged()
    {
        await _window.DispatcherQueue.EnqueueAsync(() =>
        {
            OnPropertyChanged(nameof(FeaturesLoaded));
            OnPropertyChanged(nameof(HasFeatureChanges));
            OnPropertyChanged(nameof(ChangesCanBeApplied));
        });
    }
}
