// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Templating;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Models;

public class ExtensionAdaptiveCard : IExtensionAdaptiveCard
{
    private readonly AdaptiveElementParserRegistration? _elementParserRegistration;

    private readonly AdaptiveActionParserRegistration? _actionParserRegistration;

    public event EventHandler<AdaptiveCard>? UiUpdate;

    public string DataJson { get; private set; }

    public string State { get; private set; }

    public string TemplateJson { get; private set; }

    public ExtensionAdaptiveCard(
        AdaptiveElementParserRegistration? elementParserRegistration = null,
        AdaptiveActionParserRegistration? actionParserRegistration = null)
    {
        TemplateJson = new JsonObject().ToJsonString();
        DataJson = new JsonObject().ToJsonString();
        State = string.Empty;

        _elementParserRegistration = elementParserRegistration ?? new AdaptiveElementParserRegistration();
        _actionParserRegistration = actionParserRegistration ?? new AdaptiveActionParserRegistration();
    }

    public ProviderOperationResult Update(string templateJson, string dataJson, string state)
    {
        var template = new AdaptiveCardTemplate(templateJson ?? TemplateJson);
        var adaptiveCardString = template.Expand(dataJson ?? DataJson);

        var parseResult = AdaptiveCard.FromJsonString(adaptiveCardString, _elementParserRegistration, _actionParserRegistration);

        if (parseResult.AdaptiveCard is null)
        {
            Log.Error($"ExtensionAdaptiveCard.Update(): AdaptiveCard is null - templateJson: {templateJson} dataJson: {dataJson} state: {state}");
            return new ProviderOperationResult(
                ProviderOperationStatus.Failure,
                new ArgumentNullException(null),
                "AdaptiveCard is null",
                $"templateJson: {templateJson} dataJson: {dataJson} state: {state}");
        }

        TemplateJson = templateJson ?? TemplateJson;
        DataJson = dataJson ?? DataJson;
        State = state ?? State;

        UiUpdate?.Invoke(this, parseResult.AdaptiveCard);

        return new ProviderOperationResult(
            ProviderOperationStatus.Success,
            null,
            "IExtensionAdaptiveCard.Update succeeds",
            "IExtensionAdaptiveCard.Update succeeds");
    }
}
