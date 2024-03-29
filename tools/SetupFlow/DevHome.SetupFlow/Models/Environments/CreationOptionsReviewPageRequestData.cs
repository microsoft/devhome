// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Models;
using DevHome.Common.Renderers;
using DevHome.Common.Views;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationOptionsReviewPageRequestData
{
    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; private set; }

    public string ErrorRetrievingAdaptiveCardSessionMessage { get; private set; }

    public CreationOptionsReviewPageRequestData(
        ExtensionAdaptiveCardPanel extensionAdaptiveCardPanel,
        string errorMessageLoadingAdaptiveCardMessage)
    {
        ExtensionAdaptiveCardPanel = extensionAdaptiveCardPanel;
        ErrorRetrievingAdaptiveCardSessionMessage = errorMessageLoadingAdaptiveCardMessage;
    }

    public CreationOptionsReviewPageRequestData(string errorMessageLoadingAdaptiveCardMessage)
    {
        ErrorRetrievingAdaptiveCardSessionMessage = errorMessageLoadingAdaptiveCardMessage;
    }
}
