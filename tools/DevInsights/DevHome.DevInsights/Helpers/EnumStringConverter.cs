// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevHome.DevInsights.Helpers;

#pragma warning disable SA1649 // File name should match first type name
public class EnumStringConverter<TEnum> : JsonConverter<TEnum>
#pragma warning restore SA1649 // File name should match first type name
    where TEnum : struct
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }

        var enumString = reader.GetString();
        if (Enum.TryParse(enumString, ignoreCase: true, out TEnum result))
        {
            return result;
        }

        throw new JsonException($"Unable to convert \"{enumString}\" to enum type {typeof(TEnum)}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
