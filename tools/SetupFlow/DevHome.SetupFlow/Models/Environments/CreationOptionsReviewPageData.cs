// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationOptionsReviewPageData
{
    public AdaptiveCard AdaptiveCard { get; private set; }

    public AdaptiveCardRenderer AdaptiveCardRenderer { get; private set; }

    public AdaptiveElementParserRegistration AdaptiveElementParserRegistration { get; private set; }

    public AdaptiveActionParserRegistration AdaptiveActionParserRegistration { get; private set; }

    public string SessionErrorMessage { get; private set; }

    public CreationOptionsReviewPageData(
        AdaptiveCard adaptiveCard,
        AdaptiveCardRenderer renderer,
        AdaptiveElementParserRegistration elementParserRegistration,
        AdaptiveActionParserRegistration actionParserRegistration,
        string errorMessage)
    {
        AdaptiveCard = adaptiveCard;
        SessionErrorMessage = errorMessage;
        AdaptiveCardRenderer = renderer;
        AdaptiveElementParserRegistration = elementParserRegistration;
        AdaptiveActionParserRegistration = actionParserRegistration;
    }

    public CreationOptionsReviewPageData(string errorMessage)
    {
        SessionErrorMessage = errorMessage;
    }
}
