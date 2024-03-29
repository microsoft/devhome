// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
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
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels.Environments;

/// <summary>
/// View model for the Configure Environment page in the setup flow. This page will display an adaptive card that is provided by the selected
/// compute system provider. The adaptive card will be display in the middle of the page and will contain compute system provider specific UI
/// for the user to configure their environment. The adaptive card will be provided by the compute system provider and this viewmodel will
/// stitch up the actions and elements to the adaptive card renderer.
/// </summary>
public partial class EnvironmentCreationOptionsViewModel : SetupPageViewModelBase, IRecipient<CreationProviderChangedMessage>
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly ItemsViewChoiceSet _itemsViewChoiceSet = new("SettingsCardWithButtonThatLaunchesContentDialog");

    public ComputeSystemProviderDetails CurProviderDetails { get; private set; }

    public AdaptiveCardRenderer AdaptiveCardRenderer { get; set; }

    public ComputeSystemProviderDetails UpcomingProviderDetails { get; private set; }

    public ExtensionAdaptiveCardSession ExtensionAdaptiveCardSession { get; private set; }

    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; private set; }

    [ObservableProperty]
    private bool _isAdaptiveCardSessionLoaded;

    [ObservableProperty]
    private string _errorRetrievingAdaptiveCardSessionMessage;

    public string ResultJson { get; private set; }

    public EnvironmentCreationOptionsViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        SetupFlowViewModel setupFlow)
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

        // register the supported element and action parsers
        RegisterAllSupportedDevHomeParsers();
    }

    /// <summary>
    /// Weak reference message handler for when the selected provider changes in the previous page. This will be triggered when the user
    /// selects an item in the Select Environment Provider page. The next time the user goes to this page, we'll update the UI
    /// with an adaptive card from the newly selected provider.
    /// </summary>
    /// <param name="message">Message data that contains the new provider details.</param>
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
    protected async override Task OnEachNavigateToAsync()
    {
        // This doesn't do any awaitable work, but we're overriding the method to ensure that it's called.
        // when we navigate to this page in the setup flow.
        await Task.CompletedTask;
        var curSelectedProviderId = CurProviderDetails?.ComputeSystemProvider?.Id ?? string.Empty;
        var upcomingSelectedProviderId = UpcomingProviderDetails?.ComputeSystemProvider?.Id;

        // Selected compute system provider changed so we need to update the adaptive card panel.
        // with new a adaptive card session from the new provider.
        CurProviderDetails = UpcomingProviderDetails;
        IsAdaptiveCardSessionLoaded = false;

        // Its possible that an extension could take a long time to load the adaptive card session.
        // So we run this on a background thread to prevent the UI from freezing.
        _ = Task.Run(() =>
        {
            var developerIdWrapper = CurProviderDetails.DeveloperIds.First();
            var result = CurProviderDetails.ComputeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(developerIdWrapper.DeveloperId, ComputeSystemAdaptiveCardKind.CreateComputeSystem);
            UpdateExtensionAdaptiveCardPanel(result);
        });
    }

    /// <summary>
    /// Gets and configures the adaptive card that will be displayed on the configure environment page.
    /// This adaptive card will be used through out the flow until either the user changes compute system
    /// providers, cancels the flow, or completes the flow. We use message passing to send the extension
    /// adaptive card panel to the next page of the flow when the user moves from one page to the next.
    /// </summary>
    public void UpdateExtensionAdaptiveCardPanel(ComputeSystemAdaptiveCardResult adaptiveCardSessionResult)
    {
        _dispatcher.TryEnqueue(() =>
        {
            try
            {
                // Reset error state and remove event handler from previous session.
                ExtensionAdaptiveCardPanel = null;
                ErrorRetrievingAdaptiveCardSessionMessage = null;
                if (ExtensionAdaptiveCardSession != null)
                {
                    ExtensionAdaptiveCardSession.Stopped -= OnAdaptiveCardSessionStopped;
                }

                if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
                {
                    Log.Logger()?.ReportError($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                    throw new AdaptiveCardNotRetrievedException(adaptiveCardSessionResult.Result.DisplayMessage);
                }

                ExtensionAdaptiveCardSession = new ExtensionAdaptiveCardSession(adaptiveCardSessionResult.ComputeSystemCardSession);
                ExtensionAdaptiveCardSession.Stopped += OnAdaptiveCardSessionStopped;
                AdaptiveCardRenderer = GetAdaptiveCardRenderer();
                ExtensionAdaptiveCardPanel = GetExtensionAdaptiveCardPanel(ExtensionAdaptiveCardSession, AdaptiveCardRenderer);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("EnvironmentCreationOptionsViewModel", $"Failed to get creation options adaptive card from provider {CurProviderDetails.ComputeSystemProvider.Id}.", ex);
                ErrorRetrievingAdaptiveCardSessionMessage = ex.Message;
            }

            IsAdaptiveCardSessionLoaded = true;
        });
    }

    /// <summary>
    /// The review page in the setup flow for creation will request an adaptive card panel to display the review page UI.
    /// Only this class as a reference to the ExtensionAdaptiveCardSession so all others need to request the adaptive card panel.
    /// which is created using the ExtensionAdaptiveCardSession.
    /// </summary>
    /// <param name="recipient">The class that should be receiving the request</param>
    /// <param name="message">The payload of the message request</param>
    private void OnReviewPageRequestReceived(EnvironmentCreationOptionsViewModel recipient, CreationOptionsReviewPageRequestMessage message)
    {
        // Error previously occurred, if moving to review page show the error there as well.
        if (!string.IsNullOrEmpty(ErrorRetrievingAdaptiveCardSessionMessage))
        {
            message.Reply(new CreationOptionsReviewPageRequestData(ErrorRetrievingAdaptiveCardSessionMessage));
            return;
        }

        message.Reply(new CreationOptionsReviewPageRequestData(ExtensionAdaptiveCardPanel, ErrorRetrievingAdaptiveCardSessionMessage));
    }

    /// <summary>
    /// When the extension indicates that the session has stopped, we need to get the result json from the session. Once we get this
    /// we can send a message to the EnvironmentCreationOptionsTaskGroup to let it know that the adaptive card session has ended.
    /// It will then update its setup tasks with information to create the compute system.
    /// </summary>
    /// <param name="sender">The extension session object who stopped the session</param>
    /// <param name="args">Data payload that contains the users provided input</param>
    private void OnAdaptiveCardSessionStopped(ExtensionAdaptiveCardSession sender, ExtensionAdaptiveCardSessionStoppedEventArgs args)
    {
        ResultJson = args.ResultJson;

        // Send message to the EnvironmentCreationOptionsTaskGroup to let it know that the adaptive card session has ended.
        // the task group will use the ResultJson to create the compute system.
        WeakReferenceMessenger.Default.Send(new CreationAdaptiveCardSessionEndedMessage(new CreationAdaptiveCardSessionEndedData(ResultJson, CurProviderDetails)));
        sender.Stopped -= OnAdaptiveCardSessionStopped;
    }

    /// <summary>
    /// Register all the supported DevHome adaptive card parsers that are used in the DevHome adaptive cards. All other parsers remain as default.
    /// </summary>
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
        renderer.ElementRenderers.Set(DevHomeSettingsCardChoiceSet.AdaptiveElementType, _itemsViewChoiceSet);
        renderer.ElementRenderers.Set("ActionSet", Orchestrator.DevHomeActionSetRenderer);
        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        return renderer;
    }

    private ExtensionAdaptiveCardPanel GetExtensionAdaptiveCardPanel(ExtensionAdaptiveCardSession cardSession, AdaptiveCardRenderer renderer)
    {
        var extensionPanel = new ExtensionAdaptiveCardPanel();
        extensionPanel.Bind(cardSession.Session, renderer, ElementRegistration, ActionRegistration);
        return extensionPanel;
    }
}
