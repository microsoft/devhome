// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WSLExtension.Helpers;

public static class Json
{
    public static async Task<T> ToObjectAsync<T>(string value)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)bool.Parse(value);
        }

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
        return (await JsonSerializer.DeserializeAsync<T>(stream))!;
    }

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true,
    };

    public static T? ToObject<T>(string value)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)bool.Parse(value);
        }

        return JsonSerializer.Deserialize<T>(value, _options);
    }
}
