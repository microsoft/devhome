// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace DevHome.Projects.Views;

internal class ObservableRecipientConverter : JsonConverter<ObservableRecipient>
{
    public ObservableRecipientConverter()
    {
    }

    public override ObservableRecipient ReadJson(JsonReader reader, Type objectType, ObservableRecipient existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();

    public override void WriteJson(JsonWriter writer, ObservableRecipient value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var prop in value.GetType().GetProperties())
        {
            if (prop.Name == nameof(ObservableRecipient.IsActive))
            {
                continue;
            }

            var jsonProperty = prop.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(JsonPropertyAttribute));
            var name = jsonProperty?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? prop.Name;

            writer.WritePropertyName(name);
            serializer.Serialize(writer, prop.GetValue(value));
        }

        writer.WriteEndObject();
    }
}
