// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class DevHomeContentDialogContent : IDevHomeContentDialog
{
    public string Title { get; set; } = string.Empty;

    public IAdaptiveCardElement? ContainerElement { get; set; }

    public string PrimaryButtonText { get; set; } = string.Empty;

    public string SecondaryButtonText { get; set; } = string.Empty;

    // Properties for IAdaptiveCardElement
    public string ElementTypeString { get; set; } = AdaptiveElementType;

    public static string AdaptiveElementType => "DevHome.ContentDialogContent";

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

    public JsonObject ToJson() => [];
}
