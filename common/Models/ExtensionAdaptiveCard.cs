// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Templating;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Models;

public class ExtensionAdaptiveCard : IExtensionAdaptiveCard
{
    public event EventHandler<AdaptiveCard>? UiUpdate;

    public string DataJson { get; private set; }

    public string State { get; private set; }

    public string TemplateJson { get; private set; }

    private readonly IThemeSelectorService _themeSelectorService;

    private AdaptiveCardParseResult? _parseResult;

    public ExtensionAdaptiveCard()
    {
        TemplateJson = new JsonObject().ToJsonString();
        DataJson = new JsonObject().ToJsonString();
        State = string.Empty;

        _themeSelectorService = Application.Current.GetService<IThemeSelectorService>();
        _themeSelectorService.ThemeChanged += HandleThemeChanged;
    }

    public ProviderOperationResult Update(string templateJson, string dataJson, string state)
    {
        var template = new AdaptiveCardTemplate(templateJson ?? TemplateJson);

        // Need to use Newtonsoft.Json here because System.Text.Json is missing a set of wrapping brackets
        // which causes AdaptiveCardTemplate.Expand to fail.  System.Text.Json also does not support parsing
        // an empty string.
        var adaptiveCardString = template.Expand(Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(dataJson ?? DataJson));

        _parseResult = AdaptiveCard.FromJsonString(adaptiveCardString);

        if (_parseResult.AdaptiveCard is null)
        {
            Log.Error($"ExtensionAdaptiveCard.Update(): AdaptiveCard is null - templateJson: {templateJson} dataJson: {dataJson} state: {state}");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, new ArgumentNullException(null), "AdaptiveCard is null", $"templateJson: {templateJson} dataJson: {dataJson} state: {state}");
        }

        TemplateJson = templateJson ?? TemplateJson;
        DataJson = dataJson ?? DataJson;
        State = state ?? State;

        UiUpdate?.Invoke(this, _parseResult.AdaptiveCard);

        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "IExtensionAdaptiveCard.Update succeeds", "IExtensionAdaptiveCard.Update succeeds");
    }

    private void HandleThemeChanged(object? sender, ElementTheme e)
    {
        if (_parseResult?.AdaptiveCard != null)
        {
            UiUpdate?.Invoke(this, _parseResult.AdaptiveCard);
        }
    }
}
