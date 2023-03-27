// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.Models;
public class Setting
{
    private bool _isPluginEnabled;

    private bool _isNotificationsEnabled;

    public string Path { get; }

    public string ClassId { get; }

    public string Header { get; }

    public string Description { get; }

    public bool HasToggleSwitch { get; }

    public bool IsPluginEnabled
    {
        get => _isPluginEnabled;

        set
        {
            if (_isPluginEnabled != value)
            {
                Task.Run(() =>
                {
                    var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                    return localSettingsService.SaveSettingAsync(ClassId + "-PluginDisabled", !value);
                }).Wait();

                _isPluginEnabled = value;
            }
        }
    }

    public bool IsNotificationsEnabled
    {
        get => _isNotificationsEnabled;

        set
        {
            if (_isNotificationsEnabled != value)
            {
                Task.Run(() =>
                {
                    var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                    return localSettingsService.SaveSettingAsync(ClassId + "-NotificationsDisabled", !value);
                }).Wait();

                _isNotificationsEnabled = value;
            }
        }
    }

    public Setting(string path, string classId, string header, string description, bool hasToggleSwitch)
    {
        Path = path;
        ClassId = classId;
        Header = header;
        Description = description;
        HasToggleSwitch = hasToggleSwitch;

        _isPluginEnabled = GetIsPluginEnabled();
        _isNotificationsEnabled = GetIsNotificationsEnabled();
    }

    private bool GetIsPluginEnabled()
    {
        var isDisabled = Task.Run(() =>
        {
            var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
            return localSettingsService.ReadSettingAsync<bool>(ClassId + "-PluginDisabled");
        }).Result;
        return !isDisabled;
    }

    private bool GetIsNotificationsEnabled()
    {
        var isDisabled = Task.Run(() =>
        {
            var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
            return localSettingsService.ReadSettingAsync<bool>(ClassId + "-NotificationsDisabled");
        }).Result;
        return !isDisabled;
    }
}
