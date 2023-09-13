// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.Models;
public class Setting
{
    private bool _isExtensionEnabled;

    public string Path { get; }

    public string FullName { get; }

    public string Header { get; }

    public string Description { get; }

    public string Glyph { get; }

    public bool HasToggleSwitch { get; }

    public bool HasSettingsProvider { get; }

    public bool IsExtensionEnabled
    {
        get => _isExtensionEnabled;

        set
        {
            if (_isExtensionEnabled != value)
            {
                Task.Run(() =>
                {
                    var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                    return localSettingsService.SaveSettingAsync(FullName + "-ExtensionDisabled", !value);
                }).Wait();

                _isExtensionEnabled = value;
            }
        }
    }

    public Setting(string path, string fullName, string header, string description, string glyph, bool hasToggleSwitch, bool hasSettingsProvider)
    {
        Path = path;
        FullName = fullName;
        Header = header;
        Description = description;
        Glyph = glyph;
        HasToggleSwitch = hasToggleSwitch;
        HasSettingsProvider = hasSettingsProvider;

        _isExtensionEnabled = GetIsExtensionEnabled();
    }

    private bool GetIsExtensionEnabled()
    {
        var isDisabled = Task.Run(() =>
        {
            var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
            return localSettingsService.ReadSettingAsync<bool>(FullName + "-ExtensionDisabled");
        }).Result;
        return !isDisabled;
    }
}
