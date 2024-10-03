// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevHome.Common.Services;

namespace DevHome.Common.Models.ExtensionJsonData;

/// <summary>
/// Custom JSON converter for <see cref="LocalizedProperties"/>.
/// This should be added directly to a <see cref="JsonSerializerOptions"/> to handle the conversion of a
/// localized key within the extension json to a value in the DevHome.pri resource file.
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

                if (reader.TokenType != JsonTokenType.String)
                {
                    continue;
                }

                switch (propertyName)
                {
                    case nameof(LocalizedProperties.DisplayNameKey):
                        localizedProperties.DisplayNameKey = reader.GetString()!;
                        break;
                    case nameof(LocalizedProperties.PublisherDisplayNameKey):
                        localizedProperties.PublisherDisplayNameKey = reader.GetString()!;
                        break;
                }
            }
        }

        localizedProperties.DisplayName = _stringResource.GetLocalized(localizedProperties.DisplayNameKey);
        localizedProperties.PublisherDisplayName = _stringResource.GetLocalized(localizedProperties.PublisherDisplayNameKey);

        return localizedProperties;
    }

    public override void Write(Utf8JsonWriter writer, LocalizedProperties value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
