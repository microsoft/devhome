// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace DevHome.Common.Contracts;

public interface ILocalSettingsService
{
    Task<bool> HasSettingAsync(string key);

    Task<T?> ReadSettingAsync<T>(string key, JsonTypeInfo<T> jsonTypeInfo);

    Task SaveSettingAsync<T>(string key, T value, JsonTypeInfo<T> jsonTypeInfo);
}
