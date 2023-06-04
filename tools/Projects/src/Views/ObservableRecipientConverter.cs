// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
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

            writer.WritePropertyName(prop.Name);
            serializer.Serialize(writer, prop.GetValue(value));
        }

        writer.WriteEndObject();
    }
}
