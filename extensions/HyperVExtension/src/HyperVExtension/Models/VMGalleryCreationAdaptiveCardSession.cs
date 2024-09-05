// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HyperVExtension.Common;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HyperVExtension.Models;

public enum SessionState
{
    InitialCreationForm,
    ReviewForm,
}

public class VMGalleryCreationAdaptiveCardSession : IExtensionAdaptiveCardSession2
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(VMGalleryCreationAdaptiveCardSession));

    private readonly string _pathToInitialCreationFormTemplate = Path.Combine(Package.Current.EffectivePath, Constants.HyperVTemplatesSubPath, "InitialVMGalleryCreationForm.json");

    private readonly string _pathToReviewFormTemplate = Path.Combine(Package.Current.EffectivePath, Constants.HyperVTemplatesSubPath, "ReviewFormForVMGallery.json");

    private readonly string _adaptiveCardNextButtonId = "DevHomeMachineConfigurationNextButton";

    /// <summary>
    /// The gallery images that will be displayed in the initial creation form. We retrieve these from the VMGallery.json file in the Microsoft servers.
    /// </summary>
    private readonly VMGalleryImageList _vMGalleryImageList;

    private readonly IStringResource _stringResource;

    /// <summary>
    /// Gets the Json string that represents the user input that was passed to the adaptive card session. We'll keep this so we can pass it back to Dev Home
    /// at the end of the session.
    /// </summary>
    public string OriginalUserInputJson { get; private set; } = string.Empty;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public void Dispose()
    {
    }

    public VMGalleryCreationAdaptiveCardSession(VMGalleryImageList galleryImages, IStringResource stringResource)
    {
        _vMGalleryImageList = galleryImages;
        _stringResource = stringResource;
    }

    private IExtensionAdaptiveCard? _creationAdaptiveCard;

    public bool ShouldEndSession { get; private set; }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _creationAdaptiveCard = extensionUI;

        return GetInitialCreationFormAdaptiveCard();
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(async () =>
        {
            ProviderOperationResult operationResult;
            var shouldEndSession = false;
            var adaptiveCardStateNotRecognizedError = _stringResource.GetLocalized("AdaptiveCardStateNotRecognizedError");

            var sourceGenerationContext = new AdaptiveCardActionPayloadSourceGenerationContext(Helpers.Json.Options);

            var actionPayload = Helpers.Json.ToObject<AdaptiveCardActionPayload>(action, sourceGenerationContext.AdaptiveCardActionPayload);
            if (actionPayload == null)
            {
                _log.Error($"Actions in Adaptive card action Json not recognized: {action}");
                var creationFormGenerationError = _stringResource.GetLocalized("AdaptiveCardUnRecognizedAction");
                var exception = new AdaptiveCardInvalidActionException(creationFormGenerationError);
                return new ProviderOperationResult(ProviderOperationStatus.Failure, exception, creationFormGenerationError, creationFormGenerationError);
            }

            switch (_creationAdaptiveCard?.State)
            {
                case "initialCreationForm":
                    operationResult = await HandleActionWhenFormInInitialState(actionPayload, inputs);
                    break;
                case "reviewForm":
                    (operationResult, shouldEndSession) = await HandleActionWhenFormInReviewState(actionPayload);
                    break;
                default:
                    shouldEndSession = true;
                    operationResult = new ProviderOperationResult(
                        ProviderOperationStatus.Failure,
                        new InvalidOperationException(nameof(action)),
                        adaptiveCardStateNotRecognizedError,
                        $"Unexpected state:{_creationAdaptiveCard?.State}");
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
    }

    /// <summary>
    /// Loads the adaptive card template based on the session state.
    /// </summary>
    /// <param name="state">State the adaptive card session</param>
    /// <returns>A Json string representing the adaptive card</returns>
    public string LoadTemplate(SessionState state)
    {
        var pathToTemplate = state switch
        {
            SessionState.InitialCreationForm => _pathToInitialCreationFormTemplate,
            SessionState.ReviewForm => _pathToReviewFormTemplate,
            _ => _pathToInitialCreationFormTemplate,
        };

        return File.ReadAllText(pathToTemplate, Encoding.Default);
    }

    /// <summary>
    /// Creates the initial form that will be displayed to the user. It will be a list of Windows community toolkit's settings
    /// cards that can be displayed in the Dev Homes UI.
    /// </summary>
    /// <returns>Result of the operation</returns>
    private ProviderOperationResult GetInitialCreationFormAdaptiveCard()
    {
        try
        {
            // Create the JSON array for the gallery images and add the data for each image.
            // these will be display in the initial creation form.
            var jsonArrayOfGalleryImages = new JsonArray();
            var primaryButtonForCreationFlowText = _stringResource.GetLocalized("PrimaryButtonLabelForCreationFlow");
            var secondaryButtonForCreationFlowText = _stringResource.GetLocalized("SecondaryButtonLabelForCreationFlow");
            var secondaryButtonForContentDialogText = _stringResource.GetLocalized("SecondaryButtonForContentDialogText");
            var buttonToLaunchContentDialogLabel = _stringResource.GetLocalized("ButtonToLaunchContentDialogLabel");
            var settingsCardLabel = _stringResource.GetLocalized("SettingsCardLabel");
            var enterNewVMNameLabel = _stringResource.GetLocalized("EnterNewVMNameLabel");
            var enterNewVMNamePlaceHolder = _stringResource.GetLocalized("EnterNewVMNamePlaceHolder");

            List<VMGalleryImageListImage> imageGalleryList = new();

            foreach (var image in _vMGalleryImageList.Images)
            {
                imageGalleryList.Add(new VMGalleryImageListImage(image, GetSetupContentDialogInfo(image), buttonToLaunchContentDialogLabel, secondaryButtonForContentDialogText));
            }

            var templateData =
                $"{{\"PrimaryButtonLabelForCreationFlow\" : \"{primaryButtonForCreationFlowText}\"," +
                $"\"SecondaryButtonLabelForCreationFlow\" : \"{secondaryButtonForCreationFlowText}\"," +
                $"\"SettingsCardLabel\": \"{settingsCardLabel}\"," +
                $"\"EnterNewVMNameLabel\": \"{enterNewVMNameLabel}\"," +
                $"\"EnterNewVMNamePlaceHolder\": \"{enterNewVMNamePlaceHolder}\"," +
                $"\"GalleryImages\" : {JsonSerializer.Serialize(imageGalleryList, JsonSourceGenerationContext.Default.ListVMGalleryImageListImage)}" +
                $"}}";

            var template = LoadTemplate(SessionState.InitialCreationForm);

            return _creationAdaptiveCard!.Update(template, templateData, "initialCreationForm");
        }
        catch (Exception ex)
        {
            var creationFormGenerationError = _stringResource.GetLocalized("InitialCreationFormGenerationFailedError");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, creationFormGenerationError, ex.Message);
        }
    }

    /// <summary>
    /// Creates the review form that will be displayed to the user. This will be an adaptive card that is displayed in Dev Homes
    /// setup flow review page.
    /// </summary>
    /// <returns>Result of the operation</returns>
    private async Task<ProviderOperationResult> GetForReviewFormAdaptiveCardAsync(string inputJson)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize(inputJson, JsonSourceGenerationContext.Default.VMGalleryCreationUserInput);
            var inputForGalleryOperation = deserializedObject as VMGalleryCreationUserInput ?? throw new InvalidOperationException($"Json deserialization failed for input Json: {inputJson}");

            if (inputForGalleryOperation.SelectedImageListIndex < 0 ||
                inputForGalleryOperation.SelectedImageListIndex > _vMGalleryImageList.Images.Count)
            {
                return new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Failed to get review form", "Selected image index is out of range");
            }

            var galleryImage = _vMGalleryImageList.Images[inputForGalleryOperation.SelectedImageListIndex];
            var newEnvironmentNameLabel = _stringResource.GetLocalized("NameLabelForNewVirtualMachine", ":");
            var primaryButtonForCreationFlowText = _stringResource.GetLocalized("PrimaryButtonLabelForCreationFlow");
            var secondaryButtonForCreationFlowText = _stringResource.GetLocalized("SecondaryButtonLabelForCreationFlow");
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Constants.ExtensionIconInternal));
            var randomAccessStream = await storageFile.OpenReadAsync();

            // Convert the stream to a byte array
            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);
            var providerBase64Image = Convert.ToBase64String(bytes);
            var reviewFormData = new JsonObject
            {
                { "ProviderName", HyperVStrings.HyperVProviderDisplayName },
                { "DiskImageSize", BytesHelper.ConvertBytesToString(galleryImage.Disk.SizeInBytes) },
                { "VMGalleryImageName", galleryImage.Name },
                { "Publisher", galleryImage.Publisher },
                { "NameOfNewVM", inputForGalleryOperation.NewEnvironmentName },
                { "NameLabel", newEnvironmentNameLabel },
                { "Base64ImageForProvider", providerBase64Image },
                { "DiskImageUrl", galleryImage.Symbol.Uri },
                { "PrimaryButtonLabelForCreationFlow", primaryButtonForCreationFlowText },
                { "SecondaryButtonLabelForCreationFlow", secondaryButtonForCreationFlowText },
                { "DiskImageAltText", _stringResource.GetLocalized("ImageLogoAltText", galleryImage.Name) },
            };

            var template = LoadTemplate(SessionState.ReviewForm);

            return _creationAdaptiveCard!.Update(LoadTemplate(SessionState.ReviewForm), reviewFormData.ToJsonString(), "reviewForm");
        }
        catch (Exception ex)
        {
            var reviewFormGenerationError = _stringResource.GetLocalized("ReviewFormGenerationFailedError");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, reviewFormGenerationError, ex.Message);
        }
    }

    /// <summary>
    /// The description for VM gallery images is stored in a list of strings. This method merges the strings into one string.
    /// </summary>
    /// <param name="image">The c# class that represents the gallery image</param>
    /// <returns>A string that combines the original list of strings into one</returns>
    public static string GetMergedDescription(VMGalleryImage image)
    {
        var description = string.Empty;
        for (var i = 0; i < image.Description.Count; i++)
        {
            description += image.Description[i].Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        return string.Join(string.Empty, description);
    }

    /// <summary>
    /// In Dev Homes UI, the user can click a more info button to get more information about the VM gallery image.
    /// This method sets up the content dialog's body data that will be displayed when the user clicks the more info button.
    /// </summary>
    /// <param name="image">The c# class that represents the gallery image</param>
    /// <returns>A Json object that contains the data needed to display an adaptive card within a content dialogs body</returns>
    public SetupContentDialogInfo GetSetupContentDialogInfo(VMGalleryImage image)
    {
        List<AdaptiveCardImageFact> adaptiveCardImageFacts = new();
        foreach (var fact in image.Details)
        {
            adaptiveCardImageFacts.Add(new AdaptiveCardImageFact(fact.Name, fact.Value));
        }

        adaptiveCardImageFacts.Add(new AdaptiveCardImageFact(_stringResource.GetLocalized("OsVersionForContentDialog"), image.Version));
        adaptiveCardImageFacts.Add(new AdaptiveCardImageFact(_stringResource.GetLocalized("LocaleForContentDialog"), image.Locale));
        adaptiveCardImageFacts.Add(new AdaptiveCardImageFact(_stringResource.GetLocalized("LastUpdatedForContentDialog"), image.LastUpdated.ToLongDateString()));
        adaptiveCardImageFacts.Add(new AdaptiveCardImageFact(_stringResource.GetLocalized("DownloadForContentDialog"), BytesHelper.ConvertBytesToString(image.Disk.SizeInBytes)));

        return new SetupContentDialogInfo(adaptiveCardImageFacts, GetMergedDescription(image));
    }

    private async Task<ProviderOperationResult> HandleActionWhenFormInInitialState(AdaptiveCardActionPayload actionPayload, string inputs)
    {
        ProviderOperationResult operationResult;
        var actionButtonId = actionPayload.Id ?? string.Empty;

        if (actionButtonId.Equals(_adaptiveCardNextButtonId, StringComparison.OrdinalIgnoreCase))
        {
            // if OnAction's state is initialCreationForm, then the user has selected a VM gallery image and is ready to review the form.
            // we'll also keep the original user input so we can pass it back to Dev Home once the session ends.
            OriginalUserInputJson = inputs;
            operationResult = await GetForReviewFormAdaptiveCardAsync(inputs);
        }
        else
        {
            operationResult = GetInitialCreationFormAdaptiveCard();
        }

        return operationResult;
    }

    private async Task<(ProviderOperationResult, bool)> HandleActionWhenFormInReviewState(AdaptiveCardActionPayload actionPayload)
    {
        ProviderOperationResult operationResult;
        var shouldEndSession = false;
        var actionButtonId = actionPayload.Id ?? string.Empty;

        if (actionButtonId.Equals(_adaptiveCardNextButtonId, StringComparison.OrdinalIgnoreCase))
        {
            // if OnAction's state is reviewForm, then the user has reviewed the form and Dev Home has started the creation process.
            // we'll show the same form to the user in Dev Homes summary page.
            shouldEndSession = true;
            operationResult = await GetForReviewFormAdaptiveCardAsync(OriginalUserInputJson);
        }
        else
        {
            operationResult = GetInitialCreationFormAdaptiveCard();
        }

        return (operationResult, shouldEndSession);
    }

    public class AdaptiveCardImageFact
    {
        public string Name { get; }

        public string Value { get; }

        public AdaptiveCardImageFact(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class SetupContentDialogInfo
    {
        public List<AdaptiveCardImageFact> GalleryImageFacts { get; }

        public string ImageDescription { get; }

        public SetupContentDialogInfo(List<AdaptiveCardImageFact> galleryImageFacts, string imageDescription)
        {
            GalleryImageFacts = galleryImageFacts;
            ImageDescription = imageDescription;
        }
    }

    public class VMGalleryImageListImage
    {
        public string ImageDescription { get; }

        public string SubDescription { get; }

        public string Header { get; }

        public string HeaderIcon { get; }

        public string ActionButtonText { get; }

        public SetupContentDialogInfo ContentDialogInfo { get; }

        public string ButtonToLaunchContentDialogLabel { get; }

        public string SecondaryButtonForContentDialogText { get; }

        public VMGalleryImageListImage(VMGalleryImage image, SetupContentDialogInfo contentDialogInfo, string buttonToLaunchContentDialogLabel, string secondaryButtonForContentDialogText)
        {
            ImageDescription = GetMergedDescription(image);
            SubDescription = image.Publisher;
            Header = image.Name;
            HeaderIcon = image.Symbol.Base64Image;
            ActionButtonText = "More info";
            ContentDialogInfo = contentDialogInfo;
            ButtonToLaunchContentDialogLabel = buttonToLaunchContentDialogLabel;
            SecondaryButtonForContentDialogText = secondaryButtonForContentDialogText;
        }
    }
}
