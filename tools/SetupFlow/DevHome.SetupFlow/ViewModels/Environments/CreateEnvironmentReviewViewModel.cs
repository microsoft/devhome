// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using DevHome.Common.Views;
using DevHome.SetupFlow.Models.Environments;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.SetupFlow.ViewModels.Environments;

public class CreateEnvironmentReviewViewModel : ReviewTabViewModelBase
{
    public override bool HasItems => true;

    public CreateEnvironmentReviewViewModel()
    {
    }

    /// <summary>
    /// Gets the adaptive card panel for the review page by requesting it from the EnvironmentCreationOptionsViewModel
    /// object who registered to receive the CreationOptionsReviewPageRequestMessage message.
    /// </summary>
    /// <returns>Stack panel object that contains an adaptive card as a child object</returns>
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
