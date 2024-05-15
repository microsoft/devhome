// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;

namespace DevHome.HostsFileEditor.ViewModels;

public class HostsFileEditorSettingsViewModel : ObservableObject
{
    private readonly ILocalSettingsService localSettingsService;

    private bool _showStartupWarning;

    private int _additionalLinesPosition;

    private bool _loopbackDuplicates;

    private int _encoding;

    public bool ShowStartupWarning
    {
        get
        {
            var settingName = "HostsUtilitySettings" + nameof(ShowStartupWarning);
            if (localSettingsService.HasSettingAsync(settingName).Result)
            {
                _showStartupWarning = localSettingsService.ReadSettingAsync<bool>(settingName).Result;
            }
            else
            {
                _showStartupWarning = true;
            }

            return _showStartupWarning;
        }

        set
        {
            if (_showStartupWarning != value)
            {
                _showStartupWarning = value;
                NotifyPropertyChanged(value);
            }
        }
    }

    public int AdditionalLinesPosition
    {
        get
        {
            _additionalLinesPosition = GetPropertyValueFromLocalSettings<int>(nameof(AdditionalLinesPosition));
            return _additionalLinesPosition;
        }

        set
        {
            if (_additionalLinesPosition != value)
            {
                _additionalLinesPosition = value;
                NotifyPropertyChanged(value);
            }
        }
    }

    public bool LoopbackDuplicates
    {
        get
        {
            _loopbackDuplicates = GetPropertyValueFromLocalSettings<bool>(nameof(LoopbackDuplicates));
            return _loopbackDuplicates;
        }

        set
        {
            if (_loopbackDuplicates != value)
            {
                _loopbackDuplicates = value;
                NotifyPropertyChanged(value);
            }
        }
    }

    public int Encoding
    {
        get
        {
            _encoding = GetPropertyValueFromLocalSettings<int>(nameof(Encoding));
            return _encoding;
        }

        set
        {
            if (_encoding != value)
            {
                _encoding = value;
                NotifyPropertyChanged(value);
            }
        }
    }

    public HostsFileEditorSettingsViewModel()
    {
        localSettingsService = HostsFileEditorApp.GetService<ILocalSettingsService>();
    }

    public void NotifyPropertyChanged<T>(T value, [CallerMemberName] string propertyName = null)
    {
        localSettingsService.SaveSettingAsync("HostsUtilitySettings" + propertyName, value).Wait();
    }

    private T GetPropertyValueFromLocalSettings<T>(string propertyName)
    {
        var settingName = "HostsUtilitySettings" + propertyName;
        if (localSettingsService.HasSettingAsync(settingName).Result)
        {
            var result = localSettingsService.ReadSettingAsync<T>(settingName).Result;
            return result;
        }

        return default;
    }
}
