// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using DevHome.Settings.Views;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.Devices.Display.Core;
using Windows.Storage;
using Windows.System;

namespace DevHome.Settings.ViewModels;

public partial class NotificationViewModel : ObservableRecipient
{
    private readonly Setting _setting;

    private readonly NotificationsViewModel _notificationsViewModel;

    public NotificationViewModel(Setting setting, NotificationsViewModel notificationsViewModel)
    {
        _setting = setting;
        _notificationsViewModel = notificationsViewModel;
    }

    public string Path => _setting.Path;

    public string Header => _setting.Header;

    public string Description => _setting.Description;

    public bool HasToggleSwitch => _setting.HasToggleSwitch;

    public bool IsEnabled
    {
        get => _setting.IsNotificationsEnabled;
        set => _setting.IsNotificationsEnabled = value;
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        _notificationsViewModel.Navigate(_setting.Path);
    }
}

public partial class NotificationsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private ObservableCollection<NotificationViewModel> _settingsList = new ();

    public NotificationsViewModel()
    {
        var notificationWrappers = Task.Run(() =>
        {
            var notificationService = Application.Current.GetService<IPluginService>();
            return notificationService.GetInstalledPluginsAsync(true);
        }).Result;

        var numberOfNotifications = notificationWrappers.Count();
        if (numberOfNotifications == 0)
        {
            return;
        }

        SettingsList.Clear();

        foreach (var notificationWrapper in notificationWrappers)
        {
            var setting = new Setting("Notifications/" + notificationWrapper.ClassId, notificationWrapper.ClassId, notificationWrapper.Name, string.Empty, true);
            SettingsList.Add(new NotificationViewModel(setting, this));
        }
    }

    public void Navigate(string path)
    {
        // TODO: Navigate to Notification's settings Adaptive Card
    }
}
