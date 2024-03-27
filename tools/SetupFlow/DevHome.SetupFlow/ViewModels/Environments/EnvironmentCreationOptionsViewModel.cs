// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.DevHomeAdaptiveCards.Parsers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Helpers;
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

public partial class EnvironmentCreationOptionsViewModel : SetupPageViewModelBase, IRecipient<CreationProviderChangedMessage>
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly IThemeSelectorService _themeSelectorService;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    public ComputeSystemProviderDetails CurProviderDetails { get; private set; }

    public AdaptiveCardRenderer AdaptiveCardRenderer { get; set; }

    public ComputeSystemProviderDetails UpcomingProviderDetails { get; private set; }

    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; private set; }

    public ExtensionAdaptiveCardSession ExtensionAdaptiveCardSession { get; private set; }

    public AdaptiveElementParserRegistration ElementRegistration { get; set; } = new();

    public AdaptiveActionParserRegistration ActionRegistration { get; set; } = new();

    [ObservableProperty]
    private bool _isAdaptiveCardSessionLoaded;

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
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

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
        AdaptiveCardRenderer = new AdaptiveCardRenderer();
    }

    public void Receive(CreationProviderChangedMessage message)
    {
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
        // This doesn't do any awaitable work, but we're overriding the method to ensure that it's called.
        // when we navigate to this page in the setup flow.
        await Task.CompletedTask;

        if (CurProviderDetails != UpcomingProviderDetails)
        {
            // Selected compute system provider changed so we need to update the adaptive card panel.
            // with new a adaptive card session from the new provider.
            CurProviderDetails = UpcomingProviderDetails;
            IsAdaptiveCardSessionLoaded = false;

            // Its possible that an extension could take a long time to load the adaptive card session.
            // So we run this on a background thread to prevent the UI from freezing.
            _ = Task.Run(() =>
            {
                _dispatcher.TryEnqueue(() =>
                {
                    UpdateExtensionAdaptiveCardPanel();
                    IsAdaptiveCardSessionLoaded = true;
                });
            });
        }
    }

    /// <summary>
    /// Gets and configures the adaptive card that will be displayed on the configure environment page.
    /// This adaptive card will be used through out the flow until either the user changes compute system
    /// providers, cancels the flow, or completes the flow. We use message passing to send the extension
    /// adaptive card panel to the next page of the flow when the user moves from one page to the next.
    /// </summary>
    public void UpdateExtensionAdaptiveCardPanel()
    {
        try
        {
            var developerIdWrapper = CurProviderDetails.DeveloperIds.First();
            var adaptiveCardSessionResult = CurProviderDetails.ComputeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(developerIdWrapper.DeveloperId, ComputeSystemAdaptiveCardKind.CreateComputeSystem);
            if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
            {
                Log.Logger()?.ReportError($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                return;
            }

            if (ExtensionAdaptiveCardSession != null)
            {
                ExtensionAdaptiveCardSession.Stopped -= OnAdaptiveCardSessionStopped;
            }

            ExtensionAdaptiveCardSession = new ExtensionAdaptiveCardSession(adaptiveCardSessionResult.ComputeSystemCardSession);
            ExtensionAdaptiveCardSession.Stopped += OnAdaptiveCardSessionStopped;
            AdaptiveCardRenderer = GetAdaptiveCardRenderer();
            ExtensionAdaptiveCardPanel = GetExtensionAdaptiveCardPanel(ExtensionAdaptiveCardSession, AdaptiveCardRenderer);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("EnvironmentCreationOptionsViewModel", $"Failed to get creation options adaptive card from provider {CurProviderDetails.ComputeSystemProvider.Id}.", ex);
        }
    }

    private void OnThemeChanged(object sender, ElementTheme elementTheme)
    {
        if (ExtensionAdaptiveCardPanel != null)
        {
            ExtensionAdaptiveCardPanel.RequestedTheme = elementTheme;
        }
    }

    private void OnReviewPageRequestReceived(EnvironmentCreationOptionsViewModel recipient, CreationOptionsReviewPageRequestMessage message)
    {
        var renderer = GetAdaptiveCardRenderer();
        var extensionPanel = GetExtensionAdaptiveCardPanel(ExtensionAdaptiveCardSession, renderer);
        message.Reply(new CreationOptionsReviewPageRequestData(extensionPanel));
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
        ElementRegistration.Set(DevHomeSettingsCard.AdaptiveElementType, new DevHomeSettingsCardParser());
        ElementRegistration.Set(DevHomeSettingsCardChoiceSet.AdaptiveElementType, new DevHomeSettingsCardChoiceSetParser());
        ElementRegistration.Set(DevHomeLaunchContentDialogButton.AdaptiveElementType, new DevHomeLaunchContentDialogButtonParser());
        ElementRegistration.Set(DevHomeContentDialogContent.AdaptiveElementType, new DevHomeContentDialogContentParser());
    }

    private AdaptiveCardRenderer GetAdaptiveCardRenderer()
    {
        var renderer = new AdaptiveCardRenderer();
        renderer.ElementRenderers.Set(DevHomeSettingsCardChoiceSet.AdaptiveElementType, new ItemsViewChoiceSet());
        renderer.ElementRenderers.Set("Adaptive.ActionSet", new DevHomeActionSet(TopLevelCardActionSetVisibility.Hidden));
        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        return renderer;
    }

    private ExtensionAdaptiveCardPanel GetExtensionAdaptiveCardPanel(ExtensionAdaptiveCardSession cardSession, AdaptiveCardRenderer renderer)
    {
        var extensionPanel = new ExtensionAdaptiveCardPanel();
        extensionPanel.Bind(cardSession.Session, renderer, ElementRegistration, ActionRegistration);
        extensionPanel.RequestedTheme = _themeSelectorService.GetActualTheme();
        return extensionPanel;
    }
}
