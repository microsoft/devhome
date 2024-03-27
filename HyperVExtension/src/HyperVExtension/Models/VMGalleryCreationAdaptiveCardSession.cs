// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HyperVExtension.Helpers;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using Microsoft.Windows.DevHome.SDK;
using Newtonsoft.Json.Linq;
using Windows.Foundation;

namespace HyperVExtension.Models;

public enum SessionState
{
    InitialCreationForm,
    CreationFormSubmitted,
    CreationFormFailed,
}

public class VMGalleryCreationAdaptiveCardSession : IExtensionAdaptiveCardSession2
{
    private readonly string _pathToInitialCreationFormTemplate = Path.Combine(AppContext.BaseDirectory, @"HyperVExtension\Templates\", "InitialVMGalleryCreationForm.json");

    private readonly VMGalleryImageList _vMGalleryImageList;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public void Dispose()
    {
    }

    public VMGalleryCreationAdaptiveCardSession(VMGalleryImageList galleryImages)
    {
        _vMGalleryImageList = galleryImages;
    }

    private IExtensionAdaptiveCard? _creationAdaptiveCard;

    public bool ShouldEndSession { get; private set; }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _creationAdaptiveCard = extensionUI;

        return _creationAdaptiveCard.Update(LoadTemplate(SessionState.InitialCreationForm), LoadDataForTemplate(SessionState.InitialCreationForm), "initialCreationForm");
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(() =>
        {
            ProviderOperationResult operationResult;
            var shouldEndSession = false;

            switch (_creationAdaptiveCard?.State)
            {
                case "initialCreationForm":
                    {
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
                        break;
                    }

                default:
                    {
                        shouldEndSession = true;
                        operationResult = new ProviderOperationResult(
                            ProviderOperationStatus.Failure,
                            new ArgumentNullException(nameof(action)),
                            "Adaptive card state not recognized",
                            $"Unexpected state:{_creationAdaptiveCard?.State}");
                        break;
                    }
            }

            if (shouldEndSession)
            {
                // the user input.
                Stopped?.Invoke(
                    this,
                    new ExtensionAdaptiveCardSessionStoppedEventArgs(operationResult, inputs));
            }

            return operationResult;
        }).AsAsyncOperation();
    }

    public string LoadTemplate(SessionState state)
    {
        var pathToTemplate = state switch
        {
            SessionState.InitialCreationForm => _pathToInitialCreationFormTemplate,
            _ => _pathToInitialCreationFormTemplate,
        };

        return File.ReadAllText(pathToTemplate, Encoding.Default);
    }

    public string LoadDataForTemplate(SessionState state)
    {
        var data = state switch
        {
            SessionState.InitialCreationForm => GetDataForInitialCreationForm(),
            _ => GetDataForInitialCreationForm(),
        };

        return data;
    }

    private string GetDataForInitialCreationForm()
    {
        var jsonArray = new JsonArray();

        foreach (var image in _vMGalleryImageList.Images)
        {
            var dataJson = new JsonObject
            {
                { "ImageDescription", GetMergedDescription(image) },
                { "SubDescription", image.Publisher },
                { "Header", image.Name },
                { "HeaderIcon", image.Symbol.Base64Image },
                { "ActionButtonText", "More info" },
                { "ContentDialogInfo", SetupContentDialogInfo(image) },
                { "ContentDialogPrimaryButtonText", "Previous" },
                { "ContentDialogSecondaryButtonText", "Next" },
            };

            jsonArray.Add(dataJson);
        }

        return $"{{GalleryImages : {jsonArray.ToJsonString()}}}";
    }

    public string GetMergedDescription(VMGalleryImage image)
    {
        var description = string.Empty;
        for (var i = 0; i < image.Description.Count; i++)
        {
            description += image.Description[i].Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        return string.Join(string.Empty, description);
    }

    private JsonObject SetupContentDialogInfo(VMGalleryImage image)
    {
        var imageFacts = new JsonArray();
        foreach (var fact in image.Details)
        {
            var factObj = new JsonObject
            {
                { "Title", fact.Name },
                { "Value", fact.Value },
            };
            imageFacts.Add(factObj);
        }

        imageFacts.Add(new JsonObject() { { "version", image.Version } });
        imageFacts.Add(new JsonObject() { { "locale", image.Locale } });
        imageFacts.Add(new JsonObject() { { "lastUpdated", image.LastUpdated.ToLongDateString() } });
        imageFacts.Add(new JsonObject() { { "download", BytesHelper.ConvertBytesToString(image.Disk.SizeInBytes) } });

        return new JsonObject
        {
            { "GalleryImageFacts", imageFacts },
            { "ImageDescription", GetMergedDescription(image) },
        };
    }
}
