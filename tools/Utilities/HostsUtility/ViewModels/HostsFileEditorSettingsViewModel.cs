// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
                _showStartupWarning = localSettingsService.ReadSettingAsync(settingName, HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Boolean).Result;
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
                NotifyPropertyChanged(value, HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Boolean);
            }
        }
    }

    public int AdditionalLinesPosition
    {
        get
        {
            _additionalLinesPosition = GetPropertyValueFromLocalSettings(nameof(AdditionalLinesPosition), HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Int32);
            return _additionalLinesPosition;
        }

        set
        {
            if (_additionalLinesPosition != value)
            {
                _additionalLinesPosition = value;
                NotifyPropertyChanged(value, HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Int32);
            }
        }
    }

    public bool LoopbackDuplicates
    {
        get
        {
            _loopbackDuplicates = GetPropertyValueFromLocalSettings(nameof(LoopbackDuplicates), HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Boolean);
            return _loopbackDuplicates;
        }

        set
        {
            if (_loopbackDuplicates != value)
            {
                _loopbackDuplicates = value;
                NotifyPropertyChanged(value, HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Boolean);
            }
        }
    }

    public int Encoding
    {
        get
        {
            _encoding = GetPropertyValueFromLocalSettings(nameof(Encoding), HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Int32);
            return _encoding;
        }

        set
        {
            if (_encoding != value)
            {
                _encoding = value;
                NotifyPropertyChanged(value, HostsFileEditorSettignsViewModelSourceGenerationContext.Default.Int32);
            }
        }
    }

    public HostsFileEditorSettingsViewModel()
    {
        localSettingsService = HostsFileEditorApp.GetService<ILocalSettingsService>();
    }

    public void NotifyPropertyChanged<T>(T value, JsonTypeInfo<T> jsonTypeInfo, [CallerMemberName] string propertyName = null)
    {
        localSettingsService.SaveSettingAsync("HostsUtilitySettings" + propertyName, value, jsonTypeInfo).Wait();
    }

    private T GetPropertyValueFromLocalSettings<T>(string propertyName, JsonTypeInfo<T> jsonTypeInfo)
    {
        var settingName = "HostsUtilitySettings" + propertyName;
        if (localSettingsService.HasSettingAsync(settingName).Result)
        {
            var result = localSettingsService.ReadSettingAsync<T>(settingName, jsonTypeInfo).Result;
            return result;
        }

        return default;
    }
}

[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
internal sealed partial class HostsFileEditorSettignsViewModelSourceGenerationContext : JsonSerializerContext
{
}
