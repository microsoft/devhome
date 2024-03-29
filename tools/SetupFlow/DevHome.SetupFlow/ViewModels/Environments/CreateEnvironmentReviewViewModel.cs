// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.Common.Renderers;
using DevHome.Common.Views;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.SetupFlow.ViewModels.Environments;

public partial class CreateEnvironmentReviewViewModel : ReviewTabViewModelBase
{
    private readonly ISetupFlowStringResource _stringResource;

    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; private set; }

    [ObservableProperty]
    private string _errorRetrievingAdaptiveCardSessionMessage;

    public override bool HasItems => true;

    public CreateEnvironmentReviewViewModel(
        ISetupFlowStringResource stringResource)
    {
        _stringResource = stringResource;
        TabTitle = stringResource.GetLocalized(StringResourceKey.EnvironmentCreationReviewPageTitle);
    }

    /// <summary>
    /// Gets the adaptive card panel for the review page by requesting it from the EnvironmentCreationOptionsViewModel
    /// object who registered to receive the CreationOptionsReviewPageRequestMessage message.
    /// </summary>
    public void LoadAdaptiveCardPanel()
    {
        var message = WeakReferenceMessenger.Default.Send<CreationOptionsReviewPageRequestMessage>();
        if (message.HasReceivedResponse)
        {
            ErrorRetrievingAdaptiveCardSessionMessage = message.Response.ErrorRetrievingAdaptiveCardSessionMessage;
            ExtensionAdaptiveCardPanel = message.Response.ExtensionAdaptiveCardPanel;
        }
    }
}
