// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using DevHome.Common.Contracts;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using Microsoft.Extensions.Options;
using Windows.Storage;

namespace DevHome.Common.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string DefaultApplicationDataFolder = "DevHome/ApplicationData";
    private const string DefaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localSettingsFile;

    private Dictionary<string, object> _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? DefaultApplicationDataFolder);
        _localSettingsFile = _options.LocalSettingsFile ?? DefaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read(_applicationDataFolder, _localSettingsFile, LocalSettingsServiceSourceGenerationContext.Default.DictionaryStringObject)) ?? new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<bool> HasSettingAsync(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
        }
        else
        {
            await InitializeAsync();

            if (_settings != null)
            {
                return _settings.ContainsKey(key);
            }
        }

        return false;
    }

    public async Task<T?> ReadSettingAsync<T>(string key, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Helpers.Json.ToObjectAsync<T>((string)obj, jsonTypeInfo);
            }
        }
        else
        {
            await InitializeAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
            {
                return await Helpers.Json.ToObjectAsync<T>((string)obj, jsonTypeInfo);
            }
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = await Helpers.Json.StringifyAsync(value!, jsonTypeInfo);
        }
        else
        {
            await InitializeAsync();

            _settings[key] = await Helpers.Json.StringifyAsync(value!, jsonTypeInfo);

            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localSettingsFile, _settings, LocalSettingsServiceSourceGenerationContext.Default.DictionaryStringObject));
        }
    }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal sealed partial class LocalSettingsServiceSourceGenerationContext : JsonSerializerContext
{
}
