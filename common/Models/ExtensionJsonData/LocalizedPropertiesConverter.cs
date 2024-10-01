// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevHome.Common.Services;

namespace DevHome.Common.Models.ExtensionJsonData;

/// <summary>
/// Custom JSON converter for <see cref="LocalizedProperties"/>.
/// This should be added directly to a <see cref="JsonSerializerOptions"/> to handle the conversion of the localized key.
/// </summary>
public class LocalizedPropertiesConverter : JsonConverter<LocalizedProperties>
{
    private readonly IStringResource _stringResource;

    public LocalizedPropertiesConverter(IStringResource stringResource)
    {
        _stringResource = stringResource;
    }

    public override LocalizedProperties Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var localizedProperties = new LocalizedProperties();

        // Read the JSON and populate the properties
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to the value

                if (reader.TokenType == JsonTokenType.Null)
                {
                    continue;
                }

                switch (propertyName)
                {
                    case "DisplayNameKey":
                        localizedProperties.DisplayNameKey = reader.GetString()!;
                        break;
                    case "PublisherDisplayNameKey":
                        localizedProperties.PublisherDisplayNameKey = reader.GetString()!;
                        break;
                }
            }
        }

        // Use the resource loader to populate DisplayName and PublisherDisplayName
        localizedProperties.DisplayName = _stringResource.GetLocalized(localizedProperties.DisplayNameKey);
        localizedProperties.PublisherDisplayName = _stringResource.GetLocalized(localizedProperties.PublisherDisplayNameKey);

        return localizedProperties;
    }

    public override void Write(Utf8JsonWriter writer, LocalizedProperties value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
