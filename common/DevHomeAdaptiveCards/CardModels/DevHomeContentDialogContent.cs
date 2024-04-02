// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

/// <summary>
/// Represents a content dialog that can be rendered through an adaptive card based on the
/// Json template.
/// </summary>
public class DevHomeContentDialogContent : IAdaptiveCardElement
{
    // Specific properties for DevHomeContentDialogContent
    public string Title { get; set; } = string.Empty;

    // This is the adaptive card that will be shown within
    // a content dialogs body.
    public JsonObject? ContentDialogInternalAdaptiveCardJson { get; set; }

    public string PrimaryButtonText { get; set; } = string.Empty;

    public string SecondaryButtonText { get; set; } = string.Empty;

    public static string AdaptiveElementType => "DevHome.ContentDialogContent";

    // Properties for IAdaptiveCardElement
    public string ElementTypeString { get; set; } = AdaptiveElementType;

    public JsonObject AdditionalProperties { get; set; } = new();

    public ElementType ElementType { get; set; } = ElementType.Custom;

    public IAdaptiveCardElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; }

    public HeightType Height { get; set; } = HeightType.Stretch;

    public string Id { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public IList<AdaptiveRequirement> Requirements { get; set; } = new List<AdaptiveRequirement>();

    public bool Separator { get; set; }

    public Spacing Spacing { get; set; } = Spacing.Default;

    public JsonObject? ToJson() => [];
}
