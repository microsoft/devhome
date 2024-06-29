// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using Windows.Storage;
using WSLExtension.Common;
using WSLExtension.Exceptions;
using WSLExtension.Helpers;

namespace WSLExtension.Models;

public enum SessionState
{
    WslInstallationForm,
    ReviewForm,
}

public class WslAvailableDistrosAdaptiveCardSession : IExtensionAdaptiveCardSession2
{
    private readonly string _adaptiveCardNextButtonId = "DevHomeMachineConfigurationNextButton";

    private readonly List<DistributionState> _availableDistributions;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslRegisteredDistribution));

    private const string PathToReviewFormTemplate = "ReviewFormForWslInstallation.json";
    private const string PathToWslInstallationFormTemplate = "WslInstallationForm.json";

    private readonly IStringResource _stringResource;
    private volatile IExtensionAdaptiveCard? _availableDistrosAdaptiveCard;

    public WslAvailableDistrosAdaptiveCardSession(List<DistributionState> distroList, IStringResource stringResource)
    {
        _availableDistributions = distroList;
        _stringResource = stringResource;
    }

    public string OriginalUserInputJson { get; private set; } = string.Empty;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>?
        Stopped;

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _availableDistrosAdaptiveCard = extensionUI;

        return GetWslInstallationFormAdaptiveCard();
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs) =>
        Task.Run(() =>
        {
            ProviderOperationResult operationResult;
            var shouldEndSession = false;
            var adaptiveCardStateNotRecognizedError = "Adaptive card state not recognized";

            var actionPayload = Json.ToObject<AdaptiveCardActionPayload>(action);
            if (actionPayload == null)
            {
                _log.Error($"Actions in Adaptive card action Json not recognized: {action}");
                var creationFormGenerationError =
                    "Action passed to the extension was not recognized. View the extension logs for more information";
                var exception = new AdaptiveCardInvalidActionException(creationFormGenerationError);
                return new ProviderOperationResult(
                    ProviderOperationStatus.Failure,
                    exception,
                    creationFormGenerationError,
                    creationFormGenerationError);
            }

            switch (_availableDistrosAdaptiveCard?.State)
            {
                case "wslInstallationForm":
                    operationResult = HandleActionWhenFormInInitialState(actionPayload, inputs);
                    break;
                case "reviewForm":
                    (operationResult, shouldEndSession) = HandleActionWhenFormInReviewState(actionPayload);
                    break;
                default:
                    shouldEndSession = true;
                    operationResult = new ProviderOperationResult(
                        ProviderOperationStatus.Failure,
                        new InvalidOperationException(nameof(action)),
                        adaptiveCardStateNotRecognizedError,
                        $"Unexpected state:{_availableDistrosAdaptiveCard?.State}");
                    break;
            }

            if (shouldEndSession)
            {
                // The session has now ended. We'll raise the Stopped event to notify anyone in Dev Home who was listening to this event,
                // that the session has ended.
                Stopped?.Invoke(
                    this,
                    new ExtensionAdaptiveCardSessionStoppedEventArgs(operationResult, OriginalUserInputJson));
            }

            return operationResult;
        }).AsAsyncOperation();

    public void Dispose()
    {
    }

    private ProviderOperationResult HandleActionWhenFormInInitialState(
        AdaptiveCardActionPayload actionPayload,
        string inputs)
    {
        ProviderOperationResult operationResult;
        var actionButtonId = actionPayload.Id ?? string.Empty;

        if (actionButtonId.Equals(_adaptiveCardNextButtonId, StringComparison.OrdinalIgnoreCase))
        {
            // if OnAction's state is initialCreationForm, then the user has selected a VM gallery image and is ready to review the form.
            // we'll also keep the original user input so we can pass it back to Dev Home once the session ends.
            OriginalUserInputJson = inputs;
            operationResult = GetForReviewFormAdaptiveCardAsync(inputs);
        }
        else
        {
            operationResult = GetWslInstallationFormAdaptiveCard();
        }

        return operationResult;
    }

    private (ProviderOperationResult, bool) HandleActionWhenFormInReviewState(AdaptiveCardActionPayload actionPayload)
    {
        ProviderOperationResult operationResult;
        var shouldEndSession = false;
        var actionButtonId = actionPayload.Id ?? string.Empty;

        if (actionButtonId.Equals(_adaptiveCardNextButtonId, StringComparison.OrdinalIgnoreCase))
        {
            // if OnAction's state is reviewForm, then the user has reviewed the form and Dev Home has started the creation process.
            // we'll show the same form to the user in Dev Homes summary page.
            shouldEndSession = true;
            operationResult = GetForReviewFormAdaptiveCardAsync(OriginalUserInputJson);
        }
        else
        {
            operationResult = GetWslInstallationFormAdaptiveCard();
        }

        return (operationResult, shouldEndSession);
    }

    private ProviderOperationResult GetWslInstallationFormAdaptiveCard()
    {
        try
        {
            // Create the JSON array for the gallery images and add the data for each image.
            // these will be display in the initial creation form.
            var jsonArrayOfAvailableDistributions = new JsonArray();
            var primaryButtonForCreationFlowText = "Next";
            var secondaryButtonForCreationFlowText = "Previous";

            foreach (var distribution in _availableDistributions)
            {
                var dataJson = new JsonObject
                {
                    { "Header", distribution.FriendlyName },
                    { "HeaderIcon", distribution.Base64StringLogo },
                };

                jsonArrayOfAvailableDistributions.Add(dataJson);
            }

            var templateData =
                $"{{\"PrimaryButtonLabelForCreationFlow\" : \"{primaryButtonForCreationFlowText}\"," +
                $"\"SecondaryButtonLabelForCreationFlow\" : \"{secondaryButtonForCreationFlowText}\"," +
                $"\"SettingsCardLabel\": \"Choose the WSL distribution you want to install:\"," +
                $"\"AvailableDistros\" : {jsonArrayOfAvailableDistributions.ToJsonString()}" +
                $"}}";

            var template = LoadTemplate(SessionState.WslInstallationForm);

            return _availableDistrosAdaptiveCard!.Update(template, templateData, "wslInstallationForm");
        }
        catch (Exception ex)
        {
            var creationFormGenerationError =
                _stringResource.GetLocalized("WslInstallationFormAdaptiveCardFailedError");
            return new ProviderOperationResult(
                ProviderOperationStatus.Failure,
                ex,
                creationFormGenerationError,
                ex.Message);
        }
    }

    private ProviderOperationResult GetForReviewFormAdaptiveCardAsync(string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, typeof(WslInstallationUserInput));
            var inputForWslInstallation = deserializedObject as WslInstallationUserInput ??
                                          throw new InvalidOperationException(
                                              $"Json deserialization failed for input Json: {inputJson}");

            if (inputForWslInstallation.SelectedDistroListIndex < 0 ||
                inputForWslInstallation.SelectedDistroListIndex > _availableDistributions.Count)
            {
                return new ProviderOperationResult(
                    ProviderOperationStatus.Failure,
                    null,
                    "Failed to get review form",
                    "Selected image index is out of range");
            }

            var distribution = _availableDistributions[inputForWslInstallation.SelectedDistroListIndex];
            var newDistroNameLabel = "Name:";
            var primaryButtonForCreationFlowText = _stringResource.GetLocalized("PrimaryButtonLabelForCreationFlow");
            var secondaryButtonForCreationFlowText =
                _stringResource.GetLocalized("SecondaryButtonLabelForCreationFlow");

            var reviewFormData = new JsonObject
            {
                { "ProviderName", Constants.WslProviderDisplayName },
                { "NewEnvironmentName", distribution.FriendlyName },
                { "DisplayName", distribution.DistributionName },
                { "NameLabel", newDistroNameLabel },
                { "DiskImageUrl", $"data:image/png;base64,{distribution.Base64StringLogo}" },
                { "PrimaryButtonLabelForCreationFlow", primaryButtonForCreationFlowText },
                { "SecondaryButtonLabelForCreationFlow", secondaryButtonForCreationFlowText },
            };

            // Add friendly name of the selected distribution to the original user input so Dev Home
            // can show its name on the Environments page when the environment is being created.
            OriginalUserInputJson = new JsonObject
            {
                { "NewEnvironmentName", distribution.FriendlyName },
                { "SelectedDistroListIndex", $"{inputForWslInstallation.SelectedDistroListIndex}" },
            }.ToJsonString();

            return _availableDistrosAdaptiveCard!.Update(
                LoadTemplate(SessionState.ReviewForm),
                reviewFormData.ToJsonString(),
                "reviewForm");
        }
        catch (Exception ex)
        {
            var reviewFormGenerationError = _stringResource.GetLocalized("ReviewFormGenerationFailedError");
            return new ProviderOperationResult(
                ProviderOperationStatus.Failure,
                ex,
                reviewFormGenerationError,
                ex.Message);
        }
    }

    public string LoadTemplate(SessionState state)
    {
        var pathToTemplate = state switch
        {
            SessionState.WslInstallationForm => PathToWslInstallationFormTemplate,
            SessionState.ReviewForm => PathToReviewFormTemplate,
            _ => PathToWslInstallationFormTemplate,
        };

        var task = Task.Run(async () =>
        {
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///WSLExtension/Templates/{pathToTemplate}"));
            return await FileIO.ReadTextAsync(storageFile);
        });

        task.Wait();
        return task.Result;
    }
}
