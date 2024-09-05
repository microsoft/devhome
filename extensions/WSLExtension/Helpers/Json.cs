// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace WSLExtension.Helpers;

public static class Json
{
    public static T? ToObject<T>(string value, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)bool.Parse(value);
        }

        return JsonSerializer.Deserialize<T>(value, jsonTypeInfo);
    }
}
