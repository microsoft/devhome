// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.DevHomeAdaptiveCards.Parsers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels.Environments;

public class EnvironmentCreationOptionsViewModel : SetupPageViewModelBase, IRecipient<CreationProviderChangedMessage>
{
    private readonly IThemeSelectorService _themeSelectorService;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    public ComputeSystemProviderDetails CurProviderDetails { get; private set; }

    public AdaptiveCardRenderer CurAdaptiveCardRenderer { get; set; }

    public ComputeSystemProviderDetails UpcomingProviderDetails { get; private set; }

    public AdaptiveCardRenderer UpcomingAdaptiveCardRenderer { get; set; }

    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; private set; }

    public ExtensionAdaptiveCardSession ExtensionAdaptiveCardSession { get; private set; }

    public AdaptiveElementParserRegistration ElementRegistration { get; set; } = new();

    public AdaptiveActionParserRegistration ActionRegistration { get; set; } = new();

    public string ResultJson { get; private set; }

    public EnvironmentCreationOptionsViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        SetupFlowViewModel setupFlow,
        IThemeSelectorService themeSelectorService)
           : base(stringResource, orchestrator)
    {
        PageTitle = stringResource.GetLocalized(StringResourceKey.ConfigureEnvironmentPageTitle);
        _setupFlowViewModel = setupFlow;
        _setupFlowViewModel.EndSetupFlow += OnEndSetupFlow;

        // Register for changes to the selected provider. This will be triggered when the user selects a provider.
        // from the SelectEnvironmentProviderViewModel. This is a weak reference so that the recipient can be garbage collected.
        WeakReferenceMessenger.Default.Register<CreationProviderChangedMessage>(this);

        // Register to receive the CreationOptionsReviewPageRequestMessage so that we can populate the review page with the current
        // adaptive card information.
        WeakReferenceMessenger.Default.Register<EnvironmentCreationOptionsViewModel, CreationOptionsReviewPageRequestMessage>(this, OnReviewPageRequestReceived);

        _themeSelectorService = themeSelectorService;
        _themeSelectorService.ThemeChanged += OnThemeChanged;

        // register the supported element and action parsers
        RegisterAllSupportedDevHomeParsers();
    }

    public void Receive(CreationProviderChangedMessage message)
    {
        UpcomingAdaptiveCardRenderer = message.Value.AdaptiveCardRenderer;
        UpcomingProviderDetails = message.Value.ProviderDetails;
    }

    private void OnEndSetupFlow(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _setupFlowViewModel.EndSetupFlow -= OnEndSetupFlow;
    }

    /// <summary>
    /// Make sure we only get the list of ComputeSystems from the ComputeSystemManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
        // Do nothing, but we need to override this as the base expects a task to be returned.
        await Task.CompletedTask;

        if (CurProviderDetails != UpcomingProviderDetails)
        {
            CurProviderDetails = UpcomingProviderDetails;
            CurAdaptiveCardRenderer = UpcomingAdaptiveCardRenderer;
            UpdateExtensionAdaptiveCardPanel();
        }
    }

    /// <summary>
    /// Gets and configures the UI to show to the user for logging them in.
    /// </summary>
    public void UpdateExtensionAdaptiveCardPanel()
    {
        try
        {
            var developerIdWrapper = CurProviderDetails.DeveloperIds.First();
            var adaptiveCardSessionResult = CurProviderDetails.ComputeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(developerIdWrapper.DeveloperId, ComputeSystemAdaptiveCardKind.CreateComputeSystem);
            if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
            {
                GlobalLog.Logger?.ReportError($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                return;
            }

            if (ExtensionAdaptiveCardSession != null)
            {
                ExtensionAdaptiveCardSession.Stopped -= OnAdaptiveCardSessionStopped;
            }

            ExtensionAdaptiveCardSession = new ExtensionAdaptiveCardSession(adaptiveCardSessionResult.ComputeSystemCardSession);
            ExtensionAdaptiveCardSession.Stopped += OnAdaptiveCardSessionStopped;
            CurAdaptiveCardRenderer.ElementRenderers.Set(DevHomeAdaptiveSettingsCardItemsViewChoiceSet.AdaptiveSettingsCardType, new ItemsViewChoiceSet());
            CurAdaptiveCardRenderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

            ExtensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
            ExtensionAdaptiveCardPanel.Bind(ExtensionAdaptiveCardSession.Session, CurAdaptiveCardRenderer, ElementRegistration, ActionRegistration);
            ExtensionAdaptiveCardPanel.RequestedTheme = _themeSelectorService.GetActualTheme();
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError($"GetAndUpdateExtensionAdaptiveCardPanel(): getting creation adaptive card failed.", ex);
        }
    }

    private void OnThemeChanged(object sender, ElementTheme elementTheme)
    {
        if (CurAdaptiveCardRenderer != null)
        {
            ExtensionAdaptiveCardPanel.RequestedTheme = elementTheme;
        }
    }

    private void OnReviewPageRequestReceived(EnvironmentCreationOptionsViewModel recipient, CreationOptionsReviewPageRequestMessage message)
    {
        message.Reply(new CreationOptionsReviewPageRequestData(ExtensionAdaptiveCardPanel));
    }

    private void OnAdaptiveCardSessionStopped(ExtensionAdaptiveCardSession sender, ExtensionAdaptiveCardSessionStoppedEventArgs args)
    {
        ResultJson = args.ResultJson;

        // Send message to the EnvironmentCreationOptionsTaskGroup to let it know that the adaptive card session has ended.
        // the task group will use the ResultJson to create the compute system.
        WeakReferenceMessenger.Default.Send(new CreationAdaptiveCardSessionEndedMessage(new CreationAdaptiveCardSessionEndedData(ResultJson, CurProviderDetails)));
        sender.Stopped -= OnAdaptiveCardSessionStopped;
    }

    private void RegisterAllSupportedDevHomeParsers()
    {
        ElementRegistration.Set(DevHomeAdaptiveSettingsCard.AdaptiveSettingsCardType, new DevHomeAdaptiveSettingsCardParser());
        ElementRegistration.Set(DevHomeAdaptiveSettingsCardItemsViewChoiceSet.AdaptiveSettingsCardType, new DevHomeAdaptiveSettingsCardItemsViewChoiceSetParser());
    }
}
