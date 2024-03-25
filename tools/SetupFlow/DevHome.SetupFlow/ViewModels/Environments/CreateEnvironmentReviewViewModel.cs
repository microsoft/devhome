// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Views;
using DevHome.SetupFlow.Models.Environments;

namespace DevHome.SetupFlow.ViewModels.Environments;

public class CreateEnvironmentReviewViewModel : ReviewTabViewModelBase
{
    public override bool HasItems => true;

    public CreateEnvironmentReviewViewModel()
    {
    }

    public ExtensionAdaptiveCardPanel LoadAdaptiveCardPanel()
    {
        var message = WeakReferenceMessenger.Default.Send<CreationOptionsReviewPageRequestMessage>();
        if (message.HasReceivedResponse)
        {
            return message.Response.ExtensionAdaptiveCardPanel;
        }

        return null;
    }
}
