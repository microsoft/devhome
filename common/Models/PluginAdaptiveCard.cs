// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Models;
public class PluginAdaptiveCard : IPluginAdaptiveCard
{
    public event EventHandler<AdaptiveCard>? UiUpdate;

    public string Data { get; private set; }

    public string State { get; private set; }

    public string Template { get; private set; }

    public PluginAdaptiveCard()
    {
        Template = new JsonObject().ToJsonString();
        Data = new JsonObject().ToJsonString();
        State = string.Empty;
    }

    public void Update(string template, string data, string state)
    {
        var parseResult = AdaptiveCard.FromJsonString(template);
        if (parseResult.AdaptiveCard is null)
        {
            throw new ArgumentException(JsonSerializer.Serialize(parseResult.Errors));
        }

        Template = template;
        Data = data;
        State = state;

        if (UiUpdate is not null)
        {
            UiUpdate.Invoke(this, parseResult.AdaptiveCard);
        }
    }
}
