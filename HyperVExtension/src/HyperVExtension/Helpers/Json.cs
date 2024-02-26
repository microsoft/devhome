// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HyperVExtension.Helpers;

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

    public static async Task<string> StringifyAsync<T>(T value)
    {
        if (typeof(T) == typeof(bool))
        {
            return value!.ToString()!.ToLowerInvariant();
        }

        await using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value);
        return Encoding.UTF8.GetString(stream.ToArray());
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

    public static string Stringify<T>(T value)
    {
        if (typeof(T) == typeof(bool))
        {
            return value!.ToString()!.ToLowerInvariant();
        }

        return JsonSerializer.Serialize(value, _options);
    }
}
