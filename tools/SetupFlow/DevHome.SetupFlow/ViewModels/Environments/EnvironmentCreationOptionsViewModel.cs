// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.DevHomeAdaptiveCards.Parsers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.ViewModels.Environments;

public enum CreationPageStateKind
{
    AccountSelection,
    InitialPageAdaptiveCardError,
    InitialPageAdaptiveCardLoaded,
    InitialPageAdaptiveCardLoading,
    OtherPageAdaptiveCardLoading,
    OtherPageAdaptiveCardLoaded,
}

/// <summary>
/// View model for the Configure Environment page in the setup flow. This page will display an adaptive card that is provided by the selected
/// compute system provider. The adaptive card will be display in the middle of the page and will contain compute system provider specific UI
/// for the user to configure their creation options.
/// </summary>
public partial class EnvironmentCreationOptionsViewModel : SetupPageViewModelBase, IRecipient<CreationProviderChangedMessage>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(EnvironmentCreationOptionsViewModel));

    private readonly AdaptiveCardRenderingService _adaptiveCardRenderingService;

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly AdaptiveElementParserRegistration _elementRegistration = new();

    private readonly AdaptiveActionParserRegistration _actionRegistration = new();

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly IStringResource _stringResource;

    private ComputeSystemProviderDetails _curProviderDetails;

    private AdaptiveCardRenderer _adaptiveCardRenderer;

    private ComputeSystemProviderDetails _upcomingProviderDetails;

    private ExtensionAdaptiveCardSession _extensionAdaptiveCardSession;

    private ExtensionAdaptiveCard _extensionAdaptiveCard;

    private RenderedAdaptiveCard _renderedAdaptiveCard;

    private AdaptiveInputs _userInputsFromAdaptiveCard;

    [ObservableProperty]
    private string _sessionErrorMessage;

    [ObservableProperty]
    private string _sessionErrorTitle;

    [NotifyPropertyChangedFor(nameof(DeveloperIdWrappers))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [ObservableProperty]
    private CreationPageStateKind _creationPageState;

    [ObservableProperty]
    private ObservableCollection<DeveloperIdWrapper> _developerIdWrappers = new();

    [ObservableProperty]
    private DeveloperIdWrapper _selectedDeveloperId;

    [ObservableProperty]
    private string _adaptiveCardLoadingMessage;

    public EnvironmentCreationOptionsViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        SetupFlowViewModel setupFlow,
        DispatcherQueue dispatcherQueue,
        AdaptiveCardRenderingService renderingService)
           : base(stringResource, orchestrator)
    {
        PageTitle = stringResource.GetLocalized(StringResourceKey.ConfigureEnvironmentPageTitle);
        _stringResource = stringResource;
        _setupFlowViewModel = setupFlow;
        _setupFlowViewModel.EndSetupFlow += OnEndSetupFlow;
        _dispatcherQueue = dispatcherQueue;

        // Register for changes to the selected provider. This will be triggered when the user selects a provider.
        // from the SelectEnvironmentProviderViewModel. This is a weak reference so that the recipient can be garbage collected.
        WeakReferenceMessenger.Default.Register<CreationProviderChangedMessage>(this);

        // Register to receive the EnvironmentCreationOptionsViewRequestMessage so that we can populate the configure environment page with the current
        // adaptive card information. This handles the case where the view is loaded after the view model has finished loading the adaptive card.
        WeakReferenceMessenger.Default.Register<EnvironmentCreationOptionsViewModel, CreationOptionsViewPageRequestMessage>(this, OnEnvironmentOptionsViewRequest);

        WeakReferenceMessenger.Default.Register<EnvironmentCreationOptionsViewModel, CreationOptionsReviewPageDataRequestMessage>(this, OnReviewPageViewRequest);

        // register the supported element parsers
        _elementRegistration.Set(DevHomeSettingsCard.AdaptiveElementType, new DevHomeSettingsCardParser());
        _elementRegistration.Set(DevHomeSettingsCardChoiceSet.AdaptiveElementType, new DevHomeSettingsCardChoiceSetParser());
        _elementRegistration.Set(DevHomeLaunchContentDialogButton.AdaptiveElementType, new DevHomeLaunchContentDialogButtonParser());
        _elementRegistration.Set(DevHomeContentDialogContent.AdaptiveElementType, new DevHomeContentDialogContentParser());
        _adaptiveCardRenderingService = renderingService;
        Orchestrator.CurrentSetupFlowKind = SetupFlowKind.CreateEnvironment;
    }

    /// <summary>
    /// Weak reference message handler for when the selected provider changes in the select environment provider page. This will be triggered when the user
    /// selects an item in the Select Environment Provider page. The next time the user goes to this configure environments page, we'll update the UI
    /// with an adaptive card from the newly selected provider.
    /// </summary>
    /// <param name="message">Message data that contains the new provider details.</param>
    public void Receive(CreationProviderChangedMessage message)
    {
        _upcomingProviderDetails = message.Value;
        ResetAdaptiveCardConfiguration();
    }

    private void OnEndSetupFlow(object sender, EventArgs e)
    {
        ResetAdaptiveCardConfiguration();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _setupFlowViewModel.EndSetupFlow -= OnEndSetupFlow;
        Orchestrator.CurrentSetupFlowKind = SetupFlowKind.LocalMachine;
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        _adaptiveCardRenderer = await GetAdaptiveCardRenderer();
    }

    /// <summary>
    /// Make sure we only get the list of ComputeSystems from the ComputeSystemManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    protected async override Task OnEachNavigateToAsync()
    {
        ResetErrorUI();

        // Don't get a new adaptive card session if we went from review page back to
        // configure environment creation page.
        if (Orchestrator.IsNavigatingBackward)
        {
            CreationPageState = CreationPageStateKind.InitialPageAdaptiveCardLoading;
            Orchestrator.AdaptiveCardFlowNavigator.GoToPreviousPage();
            return;
        }

        await Task.CompletedTask;

        CanGoToNextPage = false;
        var curSelectedProviderId = _curProviderDetails?.ComputeSystemProvider?.Id ?? string.Empty;
        var upcomingSelectedProviderId = _upcomingProviderDetails?.ComputeSystemProvider?.Id;

        // Selected compute system provider changed so we need to update the adaptive card in the UI
        // with a new adaptive card from the new provider.
        _curProviderDetails = _upcomingProviderDetails;
        SelectedDeveloperId = new DeveloperIdWrapper(new EmptyDeveloperId());
        AdaptiveCardLoadingMessage = StringResource.GetLocalized(StringResourceKey.EnvironmentCreationUILoadingMessage, _curProviderDetails.ComputeSystemProvider.DisplayName);
        DeveloperIdWrappers.Clear();
        CreationPageState = CreationPageStateKind.AccountSelection;

        // The DeveloperId count will always be at least one for every provider. If there is only one,
        // then the select account combo box will not be shown and the adaptive card for this provider will be shown.
        if (_curProviderDetails.DeveloperIds.Count == 1)
        {
            PopulateAdaptiveCardSectionAsync(_curProviderDetails.DeveloperIds[0].DeveloperId);
            return;
        }

        // If execution ends here, the view will only show a combo box with a list of developerIds.
        DeveloperIdWrappers = new(_curProviderDetails.DeveloperIds.OrderBy(wrapper => wrapper.LoginId));
    }

    [RelayCommand]
    public void DeveloperIdSelected(DeveloperIdWrapper wrapper)
    {
        PopulateAdaptiveCardSectionAsync(wrapper.DeveloperId);
    }

    private void PopulateAdaptiveCardSectionAsync(IDeveloperId developerId)
    {
        CreationPageState = CreationPageStateKind.InitialPageAdaptiveCardLoading;

        // Its possible that an extension could take a long time to load the adaptive card session.
        // So we run this on a background thread to prevent the UI from freezing.
        _ = Task.Run(() =>
        {
            var result = _curProviderDetails.ComputeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(
                developerId,
                ComputeSystemAdaptiveCardKind.CreateComputeSystem);

            UpdateExtensionAdaptiveCard(result);
        });
    }

    /// <summary>
    /// Gets and configures the adaptive card that will be displayed on the configure environment page.
    /// </summary>
    public void UpdateExtensionAdaptiveCard(ComputeSystemAdaptiveCardResult adaptiveCardSessionResult)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                CanGoToNextPage = false;

                // Reset error state and remove event handler from previous session.
                ResetAdaptiveCardConfiguration();

                if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
                {
                    _log.Error($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                    throw new AdaptiveCardNotRetrievedException(adaptiveCardSessionResult.Result.DisplayMessage);
                }

                // Create a new adaptive card session wrapper and add event handlers for when the session stops.
                _extensionAdaptiveCardSession = new ExtensionAdaptiveCardSession(adaptiveCardSessionResult.ComputeSystemCardSession);
                _extensionAdaptiveCardSession.Stopped += OnAdaptiveCardSessionStopped;

                // Create the Dev Home sdk extension adaptive card with our custom element and action parsers and send
                // it to the extension who will update the card with an adaptive card template and data for the template.
                // We use the OnAdaptiveCardUpdated method to update Dev Home's UI when IExtensionAdaptiveCard.Update is called.
                _extensionAdaptiveCard = new ExtensionAdaptiveCard(_elementRegistration, _actionRegistration);
                _extensionAdaptiveCard.UiUpdate += OnAdaptiveCardUpdated;

                // Initialize the adaptive card session with the extension adaptive card template and data with an initial
                // call to IExtensionAdaptiveCard.Update.
                var result = _extensionAdaptiveCardSession.Initialize(_extensionAdaptiveCard);
                if (result.Status == ProviderOperationStatus.Failure)
                {
                    _log.Error(result.ExtendedError, $"Extension failed to generate adaptive card. DisplayMsg: {result.DisplayMessage}, DiagnosticMsg: {result.DiagnosticText}");
                    SessionErrorTitle = _stringResource.GetLocalized("ErrorLoadingCreationUITitle", _curProviderDetails.ComputeSystemProvider.DisplayName);
                    SessionErrorMessage = result.DisplayMessage;
                    CanGoToNextPage = false;
                    CreationPageState = CreationPageStateKind.InitialPageAdaptiveCardError;
                }
                else
                {
                    CanGoToNextPage = true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to get creation options adaptive card from provider {_curProviderDetails.ComputeSystemProvider.Id}.");
                var displayName = _curProviderDetails.ComputeSystemProvider.DisplayName;
                SessionErrorTitle = _stringResource.GetLocalized("ErrorLoadingCreationUIForDeveloperId", displayName, SelectedDeveloperId.LoginId);
                SessionErrorMessage = ex.Message;
                CreationPageState = CreationPageStateKind.InitialPageAdaptiveCardError;
            }
        });
    }

    /// <summary>
    /// When the <see cref="DevHome.Common.Models.ExtensionAdaptiveCard"/> is updated by the extension we need to render the new adaptive card in the UI.
    /// This method does the work needed to create an adaptive card renderer, render the adaptive card and send the new adaptive card
    /// any view that is listening for the <see cref="NewAdaptiveCardAvailableMessage"/> message.
    /// </summary>
    public void OnAdaptiveCardUpdated(object sender, AdaptiveCard adaptiveCard)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Render the adaptive card and set the action event handler.
            _renderedAdaptiveCard = _adaptiveCardRenderer.RenderAdaptiveCard(adaptiveCard);
            _renderedAdaptiveCard.Action += OnRenderedAdaptiveCardAction;
            _userInputsFromAdaptiveCard = _renderedAdaptiveCard.UserInputs;

            if (ShouldAdaptiveCardAppearOnInitialPage())
            {
                SendAdaptiveCardToCurrentView();
                CreationPageState = CreationPageStateKind.InitialPageAdaptiveCardLoaded;
                CanGoToNextPage = true;
            }
            else
            {
                CreationPageState = CreationPageStateKind.OtherPageAdaptiveCardLoaded;
            }
        });
    }

    /// <summary>
    /// When the user interacts with the adaptive card by clicking the next or previous buttons in the Setup flow, we need to send
    /// the inputs and actions back to the extension. The extension will then process the inputs and actions and update the adaptive card
    /// Which will ultimately cause the <see cref="OnAdaptiveCardUpdated"/> method to be called.
    /// </summary>
    /// <param name="sender">The rendered adaptive card whose submite or execute action was just invoked </param>
    /// <param name="args">The action and user inputs from within the adaptive card</param>
    private void OnRenderedAdaptiveCardAction(object sender, AdaptiveActionEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            ResetErrorUI();
            AdaptiveCardLoadingMessage = StringResource.GetLocalized(StringResourceKey.EnvironmentCreationUILoadingMessage, _curProviderDetails.ComputeSystemProvider.DisplayName);

            // Send the inputs and actions that the user entered back to the extension.
            var result = await _extensionAdaptiveCardSession.OnAction(args.Action.ToJson().Stringify(), args.Inputs.AsJson().Stringify());
            var actionFailed = result.Status == ProviderOperationStatus.Failure;

            if (actionFailed)
            {
                _log.Error(result.ExtendedError, $"Extension failed to generate adaptive card. DisplayMsg: {result.DisplayMessage}, DiagnosticMsg: {result.DiagnosticText}");
                SessionErrorMessage = result.DisplayMessage;

                if (IsCurrentPage)
                {
                    // Make the current adaptive card in the session visible again since there was an error.
                    CreationPageState = CreationPageStateKind.InitialPageAdaptiveCardLoaded;
                }
            }

            if (IsCurrentPage && !actionFailed)
            {
                // Move Setup flow forward if the adaptive card Id that was clicked was the next button.
                if (Orchestrator.AdaptiveCardFlowNavigator.IsActionButtonIdNextButtonId(args.Action.Id))
                {
                    await Orchestrator.GoToNextPage();
                }
            }

            if (!ShouldAdaptiveCardAppearOnInitialPage())
            {
                SendAdaptiveCardToCurrentView(SessionErrorMessage);
            }
        });
    }

    private void ResetAdaptiveCardConfiguration()
    {
        ResetErrorUI();

        if (_extensionAdaptiveCardSession != null)
        {
            _extensionAdaptiveCardSession.Stopped -= OnAdaptiveCardSessionStopped;
        }

        if (_extensionAdaptiveCard != null)
        {
            _extensionAdaptiveCard.UiUpdate -= OnAdaptiveCardUpdated;
        }

        if (_renderedAdaptiveCard != null)
        {
            _renderedAdaptiveCard.Action -= OnRenderedAdaptiveCardAction;
        }
    }

    /// <summary>
    /// The configure environment view page will request an adaptive card to display in the UI if it loads after the extension sends out the CreationOptionsViewPageRequestMessage.
    /// </summary>
    /// <param name="recipient">The class that should be receiving the request</param>
    /// <param name="message">The payload of the message request</param>
    private void OnEnvironmentOptionsViewRequest(EnvironmentCreationOptionsViewModel recipient, CreationOptionsViewPageRequestMessage message)
    {
        message.Reply(_renderedAdaptiveCard);
    }

    /// <summary>
    /// The review environments view / summary page view will request an adaptive card to display in the UI if it loads after this view model sends out the original RenderedAdaptiveCard message.
    /// this can happen when the user navigates away from the review page to another page in Dev Home. E.g the settings page, then navigates back to the review page. At this point the review
    /// page is unloaded when the user navigates away from it. When they navigate back to it, a new view will be created and loaded, so we need to request the adaptive again from this view model.
    /// </summary>
    /// <param name="recipient">The class that should be receiving the request</param>
    /// <param name="message">The payload of the message request</param>
    private void OnReviewPageViewRequest(EnvironmentCreationOptionsViewModel recipient, CreationOptionsReviewPageDataRequestMessage message)
    {
        // Only send the adaptive card if the session has loaded. If the session hasn't loaded yet, we'll send an empty response. The review page should be sent the adaptive card
        // once the session has loaded in the OnAdaptiveCardUpdated method.
        var otherPageAdaptiveCardLoaded = CreationPageState == CreationPageStateKind.OtherPageAdaptiveCardLoaded;
        if (!otherPageAdaptiveCardLoaded && Orchestrator?.CurrentPageViewModel is not SummaryViewModel)
        {
            return;
        }

        message.Reply(_renderedAdaptiveCard);
    }

    /// <summary>
    /// When the extension indicates that the session has stopped, we need to get the result json from the session. Once we get this
    /// we can send a message to the CreateEnvironmentTask to let it know that the adaptive card session has ended.
    /// It will then update its setup tasks with information to create the compute system.
    /// </summary>
    /// <param name="sender">The extension session object who stopped the session</param>
    /// <param name="args">Data payload that contains the users provided input</param>
    private void OnAdaptiveCardSessionStopped(ExtensionAdaptiveCardSession sender, ExtensionAdaptiveCardSessionStoppedEventArgs args)
    {
        // Send message to the CreateEnvironmentTask to let it know that the adaptive card session has ended.
        // the task will use the ResultJson to create the compute system.
        WeakReferenceMessenger.Default.Send(new CreationAdaptiveCardSessionEndedMessage(new CreationAdaptiveCardSessionEndedData(args.ResultJson, _curProviderDetails)));
        sender.Stopped -= OnAdaptiveCardSessionStopped;
        _extensionAdaptiveCard.UiUpdate -= OnAdaptiveCardUpdated;
    }

    /// <summary>
    /// Gets the adaptive card renderer that will be used to render the adaptive card in the UI. Its important to recreate the ItemsViewChoiceSet every time we want to
    /// render an adaptive card because the parenting the ItemsView control to multiple parents will cause an exception to be thrown.
    /// </summary>
    private async Task<AdaptiveCardRenderer> GetAdaptiveCardRenderer()
    {
        var renderer = await _adaptiveCardRenderingService.GetRendererAsync();
        renderer.ElementRenderers.Set(DevHomeSettingsCardChoiceSet.AdaptiveElementType, new ItemsViewChoiceSet("SettingsCardWithButtonThatLaunchesContentDialog"));
        renderer.ElementRenderers.Set("Input.ChoiceSet", new DevHomeChoiceSetWithDynamicRefresh());

        // We need to keep the same renderer for the ActionSet that is hooked up to the orchestrator as it will have the adaptive card
        // context needed to invoke the adaptive card actions from outside the adaptive card.
        renderer.ElementRenderers.Set("ActionSet", Orchestrator.AdaptiveCardFlowNavigator.DevHomeActionSetRenderer);
        return renderer;
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    public void GoToNextPage()
    {
        ResetErrorUI();
        var validationSuccessful = Orchestrator.AdaptiveCardFlowNavigator.GoToNextPageWithValidation(_userInputsFromAdaptiveCard);

        if (validationSuccessful)
        {
            CreationPageState = CreationPageStateKind.OtherPageAdaptiveCardLoading;
        }
    }

    private void ResetErrorUI()
    {
        SessionErrorMessage = null;
        SessionErrorTitle = null;
    }

    private void SendAdaptiveCardToCurrentView(string errorMessage = null)
    {
        WeakReferenceMessenger.Default.Send(new NewAdaptiveCardAvailableMessage(
            new RenderedAdaptiveCardData(Orchestrator.CurrentPageViewModel, _renderedAdaptiveCard, errorMessage)));
    }

    private bool ShouldAdaptiveCardAppearOnInitialPage()
    {
        return CreationPageState switch
        {
            CreationPageStateKind.OtherPageAdaptiveCardLoaded => false,
            CreationPageStateKind.OtherPageAdaptiveCardLoading => false,
            _ => true,
        };
    }
}
