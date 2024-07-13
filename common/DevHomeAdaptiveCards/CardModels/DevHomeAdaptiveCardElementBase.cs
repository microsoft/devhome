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

public abstract class DevHomeAdaptiveCardElementBase : IAdaptiveCardElement
{
    public JsonObject AdditionalProperties { get; set; } = new();

    public ElementType ElementType { get; set; } = ElementType.Custom;

    public string ElementTypeString { get; set; } = string.Empty;

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
