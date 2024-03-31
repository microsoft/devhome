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
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Renderers;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.ViewModels.Environments;

/// <summary>
/// View model for the Configure Environment page in the setup flow. This page will display an adaptive card that is provided by the selected
/// compute system provider. The adaptive card will be display in the middle of the page and will contain compute system provider specific UI
/// for the user to configure their creation options.
/// </summary>
public partial class EnvironmentCreationOptionsViewModel : SetupPageViewModelBase, IRecipient<CreationProviderChangedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SelectEnvironmentProviderViewModel));

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    public ComputeSystemProviderDetails CurProviderDetails { get; private set; }

    public AdaptiveCardRenderer AdaptiveCardRenderer { get; set; }

    public ComputeSystemProviderDetails UpcomingProviderDetails { get; private set; }

    public ExtensionAdaptiveCardSession ExtensionAdaptiveCardSession { get; private set; }

    public ExtensionAdaptiveCard ExtensionAdaptiveCard { get; private set; }

    public RenderedAdaptiveCard RenderedAdaptiveCard { get; private set; }

    [ObservableProperty]
    private bool _isAdaptiveCardSessionLoaded;

    [ObservableProperty]
    private string _sessionErrorMessage;

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

        // Register to receive the EnvironmentCreationOptionsViewRequestMessage so that we can populate the configure environment page with the current
        // adaptive card information. This handles the case where the view is loaded after the view model has finished loading the adaptive card.
        WeakReferenceMessenger.Default.Register<EnvironmentCreationOptionsViewModel, CreationOptionsViewPageRequestMessage>(this, OnEnvironmentOptionsViewRequest);

        WeakReferenceMessenger.Default.Register<EnvironmentCreationOptionsViewModel, CreationOptionsReviewPageDataRequestMessage>(this, OnReviewPageViewRequest);
    }

    /// <summary>
    /// Weak reference message handler for when the selected provider changes in the select environment provider page. This will be triggered when the user
    /// selects an item in the Select Environment Provider page. The next time the user goes to this configure environments page, we'll update the UI
    /// with an adaptive card from the newly selected provider.
    /// </summary>
    /// <param name="message">Message data that contains the new provider details.</param>
    public void Receive(CreationProviderChangedMessage message)
    {
        UpcomingProviderDetails = message.Value;
    }

    private void OnEndSetupFlow(object sender, EventArgs e)
    {
        ResetAdaptiveCardConfiguration();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _setupFlowViewModel.EndSetupFlow -= OnEndSetupFlow;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        // Does nothing, but we need to override this as the base expects a task to be returned.
        await Task.CompletedTask;
        AdaptiveCardRenderer = GetAdaptiveCardRenderer();
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

        // Selected compute system provider  may havechanged so we need to update the adaptive card in the UI
        // with new a adaptive card from the new provider.
        CurProviderDetails = UpcomingProviderDetails;

        IsAdaptiveCardSessionLoaded = false;

        // Its possible that an extension could take a long time to load the adaptive card session.
        // So we run this on a background thread to prevent the UI from freezing.
        _ = Task.Run(() =>
        {
            var developerIdWrapper = CurProviderDetails.DeveloperIds.First();
            var result = CurProviderDetails.ComputeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(developerIdWrapper.DeveloperId, ComputeSystemAdaptiveCardKind.CreateComputeSystem);
            UpdateExtensionAdaptiveCard(result);
        });
    }

    /// <summary>
    /// Gets and configures the adaptive card that will be displayed on the configure environment page.
    /// </summary>
    public void UpdateExtensionAdaptiveCard(ComputeSystemAdaptiveCardResult adaptiveCardSessionResult)
    {
        _dispatcher.TryEnqueue(() =>
        {
            try
            {
                // Reset error state and remove event handler from previous session.
                ResetAdaptiveCardConfiguration();

                if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
                {
                    _log.Error($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                    throw new AdaptiveCardNotRetrievedException(adaptiveCardSessionResult.Result.DisplayMessage);
                }

                ExtensionAdaptiveCardSession = new ExtensionAdaptiveCardSession(adaptiveCardSessionResult.ComputeSystemCardSession);
                ExtensionAdaptiveCardSession.Stopped += OnAdaptiveCardSessionStopped;
                ExtensionAdaptiveCard = new ExtensionAdaptiveCard(ElementRegistration, ActionRegistration);
                ExtensionAdaptiveCard.UiUpdate += OnAdaptiveCardUpdated;

                ExtensionAdaptiveCardSession.Initialize(ExtensionAdaptiveCard);
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to get creation options adaptive card from provider {CurProviderDetails.ComputeSystemProvider.Id}.", ex);
                SessionErrorMessage = ex.Message;
            }
        });
    }

    public void OnAdaptiveCardUpdated(object sender, AdaptiveCard adaptiveCard)
    {
        _dispatcher.TryEnqueue(async () =>
        {
            RenderedAdaptiveCard = GetAdaptiveCardRenderer().RenderAdaptiveCard(adaptiveCard);
            RenderedAdaptiveCard.Action += OnRenderedAdaptiveCardAction;

            // Add a small delay here for the case where the setup flow is switching between the configure environment page and the review page.
            await Task.Delay(250);

            // Send new card to listeners
            if (Orchestrator.IsCurrentPage(this))
            {
                UserInputsFromAdaptiveCard = RenderedAdaptiveCard.UserInputs;
                WeakReferenceMessenger.Default.Send(new CreationOptionsConfigureEnvironmentMessage(RenderedAdaptiveCard));
            }
            else
            {
                // To prevent the rendered adaptive card from crashing due to being added as a child of multiple UI elements in the visual tree, the recipient of this message will reconstruct
                // a new the adaptive card with data from the currently rendered adaptive card. This is needed so the review page can display the adaptive card from the extension after we move
                // from the configure environment page to the review page.
                WeakReferenceMessenger.Default.Send(new CreationOptionsReviewPageData(RenderedAdaptiveCard?.OriginatingCard, GetAdaptiveCardRenderer(), ElementRegistration, ActionRegistration, SessionErrorMessage));
            }

            IsAdaptiveCardSessionLoaded = true;
        });
    }

    private void OnRenderedAdaptiveCardAction(object sender, AdaptiveActionEventArgs args)
    {
        _dispatcher.TryEnqueue(async () =>
        {
            IsAdaptiveCardSessionLoaded = false;

            // Send the inputs and actions that the user entered back to the extension.
            await ExtensionAdaptiveCardSession.OnAction(args.Action.ToJson().Stringify(), args.Inputs.AsJson().Stringify());
        });
    }

    private void ResetAdaptiveCardConfiguration()
    {
        SessionErrorMessage = null;
        if (ExtensionAdaptiveCardSession != null)
        {
            ExtensionAdaptiveCardSession.Stopped -= OnAdaptiveCardSessionStopped;
        }

        if (ExtensionAdaptiveCard != null)
        {
            ExtensionAdaptiveCard.UiUpdate -= OnAdaptiveCardUpdated;
        }

        if (RenderedAdaptiveCard != null)
        {
            RenderedAdaptiveCard.Action -= OnRenderedAdaptiveCardAction;
        }
    }

    /// <summary>
    /// The configure environment view page will request an adaptive card to display in the UI if it loads after the extension sends out the CreationOptionsViewPageRequestMessage.
    /// </summary>
    /// <param name="recipient">The class that should be receiving the request</param>
    /// <param name="message">The payload of the message request</param>
    private void OnEnvironmentOptionsViewRequest(EnvironmentCreationOptionsViewModel recipient, CreationOptionsViewPageRequestMessage message)
    {
        message.Reply(RenderedAdaptiveCard);
    }

    /// <summary>
    /// The review environments view page will request an adaptive card to display in the UI if it loads after this view model sends out the original CreationOptionsReviewPageData message.
    /// this can happen when the user navigates away from the review page to another page in Dev Home. E.g the settings page, then navigates back to the review page. At this point the review
    /// page is unloaded when the user navigates away from it. When they navigate back to it, a new view will be created and loaded, so we need to request the adaptive again from this view model.
    /// </summary>
    /// <param name="recipient">The class that should be receiving the request</param>
    /// <param name="message">The payload of the message request</param>
    private void OnReviewPageViewRequest(EnvironmentCreationOptionsViewModel recipient, CreationOptionsReviewPageDataRequestMessage message)
    {
        // Only send the adaptive card if the session has loaded. If the session hasn't loaded yet, we'll send an empty response. The review page should be sent the adaptive card
        // once the session has loaded in the OnAdaptiveCardUpdated method.
        if (!IsAdaptiveCardSessionLoaded)
        {
            return;
        }

        message.Reply(new CreationOptionsReviewPageData(RenderedAdaptiveCard?.OriginatingCard, GetAdaptiveCardRenderer(), ElementRegistration, ActionRegistration, SessionErrorMessage));
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
    /// Gets the adaptive card renderer that will be used to render the adaptive card in the UI. Its important to recreate the ItemsViewChoiceSet every time we want to
    /// render an adaptive card because the parenting the ItemsView control to multiple parents will cause an exception to be thrown.
    /// </summary>
    private AdaptiveCardRenderer GetAdaptiveCardRenderer()
    {
        var renderer = new AdaptiveCardRenderer();
        renderer.ElementRenderers.Set(DevHomeSettingsCardChoiceSet.AdaptiveElementType, new ItemsViewChoiceSet("SettingsCardWithButtonThatLaunchesContentDialog"));
        renderer.ElementRenderers.Set("ActionSet", Orchestrator.DevHomeActionSetRenderer);
        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        return renderer;
    }
}
