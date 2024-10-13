// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Environments.Models;

public class EnvironmentsCallToActionData
{
    public string CallToActionText { get; }

    public string CallToActionHyperLinkText { get; }

    public EnvironmentsCallToActionData(string callToActionText, string callToActionHyperLinkText)
    {
        CallToActionText = callToActionText;
        CallToActionHyperLinkText = callToActionHyperLinkText;
    }
}
