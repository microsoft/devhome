// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using DevHome.Customization.TelemetryEvents;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class GeneralSystemViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GeneralSystemViewModel));

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly StringResource _stringResource;

    private readonly ShellSettings _shellSettings;

    private readonly bool _isUserAdministrator = WindowsIdentityHelper.IsUserAdministrator();

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    private StackedNotificationsBehavior? _notificationQueue;

    private AsyncRelayCommand<bool> ModifyLongPathsCommand { get; }

    public bool CanModifyLongPaths => _isUserAdministrator && !ModifyLongPathsCommand.IsRunning;

    public GeneralSystemViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
        _shellSettings = new ShellSettings();

        _stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(_stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(_stringResource.GetLocalized("GeneralSystem_Header"), typeof(GeneralSystemViewModel).FullName!)
        ];

        ModifyLongPathsCommand = new AsyncRelayCommand<bool>(ModifyLongPathsAsync);
        ModifyLongPathsCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ModifyLongPathsCommand.IsRunning))
            {
                await _dispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(CanModifyLongPaths)));
            }
        };
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;
    }

    public void Uninitialize()
    {
        _notificationQueue = null;
    }

    public bool EndTaskOnTaskBarEnabled
    {
        get => _shellSettings.EndTaskOnTaskBarEnabled();
        set
        {
            SettingChangedEvent.Log("EndTaskOnTaskBarEnabled", value.ToString());
            _shellSettings.SetEndTaskOnTaskBarEnabled(value);
        }
    }

    public bool LongPathsEnabled
    {
        get => CheckLongPathsEnabled();
        set
        {
            if (ModifyLongPathsCommand.IsRunning)
            {
                return;
            }

            ModifyLongPathsCommand.ExecuteAsync(value);
        }
    }

    private bool CheckLongPathsEnabled()
    {
        const string keyPath = @"SYSTEM\CurrentControlSet\Control\FileSystem";
        const string valueName = "LongPathsEnabled";

        using var key = Registry.LocalMachine.OpenSubKey(keyPath);
        if (key != null)
        {
            var value = key.GetValue(valueName);
            if (value is int intValue)
            {
                return intValue == 1;
            }
        }

        return false;
    }

    private async Task ModifyLongPathsAsync(bool enabled)
    {
        await Task.Run(async () =>
        {
            var currentState = CheckLongPathsEnabled();
            if (enabled == currentState)
            {
                return;
            }

            var exitCode = ModifyLongPathsSetting.ModifyLongPaths(enabled, _log);
            if (exitCode == ModifyLongPathsSetting.ExitCode.Success)
            {
                _log?.Information($"Long paths setting {(enabled ? "enabled" : "disabled")} successfully.");
                ShowRestartNotification();
            }
            else
            {
                _log?.Error($"Failed to {(enabled ? "enable" : "disable")} long paths setting.");
            }

            await _dispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(LongPathsEnabled)));
        });
    }

    public void ShowRestartNotification()
    {
        _notificationQueue?.ShowWithWindowExtension(
            _stringResource.GetLocalized("ChangesAppliedTitle"),
            _stringResource.GetLocalized("LongPathsChangedRestartMessage"),
            InfoBarSeverity.Warning,
            new RelayCommand(RestartHelper.RestartComputer),
            _stringResource.GetLocalized("RestartNowButtonText"));
    }
}
