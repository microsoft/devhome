// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Common.Views;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationOptionsReviewPageRequestData
{
    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; private set; }

    public CreationOptionsReviewPageRequestData(ExtensionAdaptiveCardPanel extensionAdaptiveCardPanel)
    {
        ExtensionAdaptiveCardPanel = extensionAdaptiveCardPanel;
    }
}
