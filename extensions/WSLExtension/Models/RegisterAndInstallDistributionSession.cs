// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.DistributionDefinitions;
using WSLExtension.Exceptions;
using WSLExtension.Helpers;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public enum SessionState
{
    WslInstallationForm,
    ReviewForm,
}

/// <summary>
/// Class used to send adaptive cards to Dev Home's machine create environment flow. It sends a list of available
/// wsl distributions on the first page, and then the name and logo of the selected distribution on the review page.
/// </summary>
public class RegisterAndInstallDistributionSession : IExtensionAdaptiveCardSession2
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RegisterAndInstallDistributionSession));

    private readonly PackageHelper _packageHelper = new();

    private readonly string _adaptiveCardNextButtonId = "DevHomeMachineConfigurationNextButton";

    private readonly string _pathToInitialCreationFormTemplate =
        Path.Combine(Package.Current.EffectivePath, $@"{WslTemplateSubfolderName}\WslInstallationForm.json");

    private readonly string _pathToReviewFormTemplate =
        Path.Combine(Package.Current.EffectivePath, $@"{WslTemplateSubfolderName}\ReviewFormForWslInstallation.json");

    private readonly List<DistributionDefinition> _availableDistributionsToInstall;

    private readonly IStringResource _stringResource;

    private string? _defaultWslLogo;

    private IExtensionAdaptiveCard? _availableDistributionsAdaptiveCard;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public string UserInputJson { get; private set; } = string.Empty;

    public RegisterAndInstallDistributionSession(List<DistributionDefinition> availableDistributions, IStringResource stringResource)
    {
        _availableDistributionsToInstall = availableDistributions;
        _stringResource = stringResource;
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _availableDistributionsAdaptiveCard = extensionUI;
        _defaultWslLogo ??= _packageHelper.GetBase64StringFromLogoPathAsync(DefaultWslLogoPath).GetAwaiter().GetResult();

        return GetWslInstallationFormAdaptiveCard();
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(() =>
        {
            try
            {
                ProviderOperationResult operationResult;
                var shouldEndSession = false;
                var adaptiveCardStateNotRecognizedError = _stringResource.GetLocalized("AdaptiveCardStateNotRecognizedError");

                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    IncludeFields = true,
                };

                var temp = new AdaptiveCardActionPayloadSourceGenerationContext(options);

                var actionPayload = Json.ToObject<AdaptiveCardActionPayload>(action, temp.AdaptiveCardActionPayload);
                if (actionPayload == null)
                {
                    _log.Error($"Actions in Adaptive card action Json not recognized: {action}");
                    var creationFormGenerationError = _stringResource.GetLocalized("AdaptiveCardUnRecognizedAction");
                    throw new AdaptiveCardInvalidActionException(creationFormGenerationError);
                }

                switch (_availableDistributionsAdaptiveCard?.State)
                {
                    case "wslInstallationForm":
                        operationResult = HandleActionWhenFormInInitialState(actionPayload, inputs);
                        break;
                    case "reviewForm":
                        (operationResult, shouldEndSession) = HandleActionWhenFormInReviewState(actionPayload);
                        break;
                    default:
                        _log.Error($"No matching state found for: '{_availableDistributionsAdaptiveCard?.State}'." +
                            $"resetting state of adaptive card back to default.");
                        operationResult = GetWslInstallationFormAdaptiveCard();
                        break;
                }

                if (shouldEndSession)
                {
                    // The session has now ended. We'll raise the Stopped event to notify anyone in Dev Home who was listening to this event,
                    // that the session has ended.
                    Stopped?.Invoke(
                        this,
                        new ExtensionAdaptiveCardSessionStoppedEventArgs(operationResult, UserInputJson));
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to process action request from Dev Home");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
            }
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Loads the adaptive card template based on the session state.
    /// </summary>
    /// <param name="state">State of the adaptive card session</param>
    /// <returns>A Json string representing the adaptive card</returns>
    public string LoadTemplate(SessionState state)
    {
        var pathToTemplate = state switch
        {
            SessionState.WslInstallationForm => _pathToInitialCreationFormTemplate,
            SessionState.ReviewForm => _pathToReviewFormTemplate,
            _ => _pathToInitialCreationFormTemplate,
        };

        return File.ReadAllText(pathToTemplate, Encoding.Default);
    }

    private ProviderOperationResult HandleActionWhenFormInInitialState(AdaptiveCardActionPayload actionPayload, string inputs)
    {
        ProviderOperationResult operationResult;
        var actionButtonId = actionPayload.Id ?? string.Empty;

        if (actionButtonId.Equals(_adaptiveCardNextButtonId, StringComparison.OrdinalIgnoreCase))
        {
            // if OnAction's state is initialCreationForm, then the user has selected a VM gallery image and is ready to review the form.
            // we'll also keep the original user input so we can pass it back to Dev Home once the session ends.
            UserInputJson = inputs;
            operationResult = GetWslReviewFormAdaptiveCardAsync(inputs);
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
            operationResult = GetWslReviewFormAdaptiveCardAsync(UserInputJson);
        }
        else
        {
            operationResult = GetWslInstallationFormAdaptiveCard();
        }

        return (operationResult, shouldEndSession);
    }

    /// <summary>
    /// Creates the initial form that will be displayed to the user. It will be a list of settings cards from the
    /// Windows community toolkit that can be displayed in Dev Homes UI. This will be display on the initial
    /// create environments page once the user selects the wsl extension and clicks the next button.
    /// </summary>
    /// <returns>Result of the operation</returns>
    private ProviderOperationResult GetWslInstallationFormAdaptiveCard()
    {
        try
        {
            // Create the JSON array for the available wsl distributions that can be installed and
            // add the data for each one. These will be display in the initial creation form.
            List<AvailableDistributions> jsonArrayOfAvailableDistributions = new();
            var primaryButtonForCreationFlowText = _stringResource.GetLocalized("PrimaryButtonLabelForCreationFlow");
            var secondaryButtonForCreationFlowText = _stringResource.GetLocalized("SecondaryButtonLabelForCreationFlow");
            var settingsCardLabel = _stringResource.GetLocalized("SettingsCardLabel", _availableDistributionsToInstall.Count);
            var noDistributionsFound = _stringResource.GetLocalized("NoDistributionsFoundAvailable");

            // Add information about all found distributions so we can use them in the settings cards
            foreach (var distribution in _availableDistributionsToInstall)
            {
                var base64Logo =
                    string.IsNullOrEmpty(distribution.Base64StringLogo) ? _defaultWslLogo : distribution.Base64StringLogo;

                jsonArrayOfAvailableDistributions.Add(new AvailableDistributions(distribution.FriendlyName, base64Logo, distribution.Publisher));
            }

            List<NoDistributionErrorData> noDistributionErrorDatas = new()
            {
                new NoDistributionErrorData(noDistributionsFound, _availableDistributionsToInstall.Count == 0),
            };

            var templateData =
                $"{{\"PrimaryButtonLabelForCreationFlow\" : \"{primaryButtonForCreationFlowText}\"," +
                $"\"SecondaryButtonLabelForCreationFlow\" : \"{secondaryButtonForCreationFlowText}\"," +
                $"\"SettingsCardLabel\": \"{settingsCardLabel}\"," +
                $"\"NoDistributionErrorData\": {JsonSerializer.Serialize(noDistributionErrorDatas, DistributionSessionSourceGenerationContext.Default.NoDistributionErrorData)}," +
                $"\"AvailableDistributions\" : {JsonSerializer.Serialize(jsonArrayOfAvailableDistributions, DistributionSessionSourceGenerationContext.Default.AvailableDistributions)}" +
                $"}}";

            var template = LoadTemplate(SessionState.WslInstallationForm);

            return _availableDistributionsAdaptiveCard!.Update(template, templateData, "wslInstallationForm");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unable to create wsl creation form due to error");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
        }
    }

    /// <summary>
    /// Creates the review form that will be displayed to the user. This will be an adaptive card that
    /// is displayed in Dev Homes setup flow review page.
    /// </summary>
    /// <returns>Result of the operation</returns>
    private ProviderOperationResult GetWslReviewFormAdaptiveCardAsync(string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, WslInstallationUserInputSourceGenerationContext.Default.WslInstallationUserInput);

            if (!(deserializedObject is WslInstallationUserInput inputForWslInstallation))
            {
                throw new InvalidOperationException($"Json deserialization failed for input Json: {inputJson}");
            }

            var distribution = _availableDistributionsToInstall[inputForWslInstallation.SelectedDistributionIndex];
            var distributionLabel = _stringResource.GetLocalized("DistributionNameLabel");
            var publisherLabel = _stringResource.GetLocalized("ReviewPagePublisherLabel");
            var primaryButtonForCreationFlowText = _stringResource.GetLocalized("PrimaryButtonLabelForCreationFlow");
            var secondaryButtonForCreationFlowText = _stringResource.GetLocalized("SecondaryButtonLabelForCreationFlow");
            var extensionLabel = _stringResource.GetLocalized("ExtensionLabel");
            var base64Logo = string.IsNullOrEmpty(distribution.Base64StringLogo) ? _defaultWslLogo : distribution.Base64StringLogo;

            var reviewFormData = new JsonObject
            {
                { "ProviderName", WslProviderDisplayName },
                { "NewEnvironmentName", distribution.FriendlyName },
                { "DistributionName", distribution.Name },
                { "DistributionNameLabel", distributionLabel },
                { "PublisherName", distribution.Publisher },
                { "ReviewPagePublisherLabel", publisherLabel },
                { "ExtensionLabel", extensionLabel },
                { "DistributionImage", $"data:image/png;base64,{base64Logo}" },
                { "PrimaryButtonLabelForCreationFlow", primaryButtonForCreationFlowText },
                { "SecondaryButtonLabelForCreationFlow", secondaryButtonForCreationFlowText },
                { "DistributionImageLogoAltText", _stringResource.GetLocalized("DistributionLogoAltText", distribution.FriendlyName) },
            };

            // Add friendly name of the selected distribution to the original user input so Dev Home
            // can show its name on the Environments page when the environment is being created.
            UserInputJson = new JsonObject
            {
                { "NewEnvironmentName", distribution.FriendlyName },
                { "SelectedDistributionIndex", $"{inputForWslInstallation.SelectedDistributionIndex}" },
            }.ToJsonString();

            return _availableDistributionsAdaptiveCard!.Update(
                LoadTemplate(SessionState.ReviewForm),
                reviewFormData.ToJsonString(),
                "reviewForm");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unable to create wsl review form due to error");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
        }
    }

    public void Dispose()
    {
    }
}

public class AvailableDistributions
{
    public string Header { get; }

    public string? HeaderIcon { get; set; }

    public string PublisherName { get; set; }

    public AvailableDistributions(string header, string? headerIcon, string publisherName)
    {
        Header = header;
        HeaderIcon = headerIcon;
        PublisherName = publisherName;
    }
}

public class NoDistributionErrorData
{
    public string? NoDistributionsFoundError { get; }

    public bool NoDistributionsFoundErrorVisibility { get; }

    public NoDistributionErrorData(string? noDistributionsFoundError, bool noDistributionsFoundErrorVisibility)
    {
        NoDistributionsFoundError = noDistributionsFoundError;
        NoDistributionsFoundErrorVisibility = noDistributionsFoundErrorVisibility;
    }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AvailableDistributions))]
[JsonSerializable(typeof(NoDistributionErrorData))]
internal sealed partial class DistributionSessionSourceGenerationContext : JsonSerializerContext
{
}
