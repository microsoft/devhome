// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Templating;
using DevHome.Logging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Models;

public class ExtensionAdaptiveCard : IExtensionAdaptiveCard
{
    private readonly AdaptiveElementParserRegistration? _elementParserRegistration;

    private readonly AdaptiveActionParserRegistration? _actionParserRegistration;

    public event EventHandler<AdaptiveCard>? UiUpdate;

    public string DataJson { get; private set; }

    public string State { get; private set; }

    public string TemplateJson { get; private set; }

    public AdaptiveCardParseResult? LastParseResult { get; private set; }

    public ExtensionAdaptiveCard()
    {
        TemplateJson = new JsonObject().ToJsonString();
        DataJson = new JsonObject().ToJsonString();
        State = string.Empty;
    }

    public ExtensionAdaptiveCard(AdaptiveElementParserRegistration elementParserRegistration, AdaptiveActionParserRegistration actionParserRegistration)
        : this()
    {
        _elementParserRegistration = elementParserRegistration;
        _actionParserRegistration = actionParserRegistration;
    }

    public ProviderOperationResult Update(string templateJson, string dataJson, string state)
    {
        var template = new AdaptiveCardTemplate(templateJson ?? TemplateJson);

        // Need to use Newtonsoft.Json here because System.Text.Json is missing a set of wrapping brackets
        // which causes AdaptiveCardTemplate.Expand to fail.  System.Text.Json also does not support parsing
        // an empty string.
        var adaptiveCardString = template.Expand(Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(dataJson ?? DataJson));

        AdaptiveCardParseResult parseResult;

        if (_actionParserRegistration != null && _elementParserRegistration != null)
        {
            parseResult = AdaptiveCard.FromJsonString(adaptiveCardString, _elementParserRegistration, _actionParserRegistration);
        }
        else
        {
            parseResult = AdaptiveCard.FromJsonString(adaptiveCardString);
        }

        if (parseResult.AdaptiveCard is null)
        {
            GlobalLog.Logger?.ReportError($"ExtensionAdaptiveCard.Update(): AdaptiveCard is null - templateJson: {templateJson} dataJson: {dataJson} state: {state}");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, new ArgumentNullException(null), "AdaptiveCard is null", $"templateJson: {templateJson} dataJson: {dataJson} state: {state}");
        }

        TemplateJson = templateJson ?? TemplateJson;
        DataJson = dataJson ?? DataJson;
        State = state ?? State;
        LastParseResult = parseResult;

        UiUpdate?.Invoke(this, parseResult.AdaptiveCard);

        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "IExtensionAdaptiveCard.Update succeeds", "IExtensionAdaptiveCard.Update succeeds");
    }
}
