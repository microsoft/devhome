// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;
using Windows.UI;

namespace DevHome.Common.Renderers;

public class LabelGroup : IAdaptiveCardElement
{
    public LabelGroup()
    {
        Labels = new List<(string, string)>();
    }

    public List<(string, string)> Labels { get; set; }

    public bool RoundedCorners { get; set; } = true;

    public static readonly string CustomTypeString = "LabelGroup";

    public JsonObject ToJson()
    {
        return new JsonObject();
    }

    public JsonObject? AdditionalProperties { get; set; }

    public ElementType ElementType => ElementType.Custom;

    public string ElementTypeString => CustomTypeString;

    public IAdaptiveCardElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; }

    public HeightType Height { get; set; } = HeightType.Auto;

    public string? Id { get; set; } = CustomTypeString + "Id";

    public bool IsVisible { get; set; } = true;

    public bool Separator { get; set; }

    public Spacing Spacing { get; set; } = Spacing.Small;

    IList<AdaptiveRequirement>? IAdaptiveCardElement.Requirements { get; }
}

public class LabelGroupParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(
        JsonObject inputJson,
        AdaptiveElementParserRegistration elementParsers,
        AdaptiveActionParserRegistration actionParsers,
        IList<AdaptiveWarning> warnings)
    {
        var labelGroup = new LabelGroup();

        // Parse the labels and their colors.
        var labels = inputJson.GetNamedArray("labels", null);
        if (labels is not null)
        {
            foreach (var label in labels)
            {
                var obj = label.GetObject();
                if (obj != null)
                {
                    var text = obj.GetNamedString("text", string.Empty).ToString();
                    var color = obj.GetNamedString("color", string.Empty).ToString();
                    labelGroup.Labels.Add((text, color));
                }
            }
        }

        // Parse whether the displayed labels should have rounded corners. If not specified, default to true.
        var roundedCorners = inputJson.GetNamedBoolean("roundedCorners", true);
        labelGroup.RoundedCorners = roundedCorners;

        return labelGroup;
    }
}

public class LabelGroupRenderer : IAdaptiveElementRenderer
{
    public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var wrapPanel = new WrapPanel
        {
            Name = LabelGroup.CustomTypeString,
            Orientation = Orientation.Horizontal,
            HorizontalSpacing = 4,
            VerticalSpacing = 4,
        };

        if (element is LabelGroup labelGroup)
        {
            var labels = labelGroup.Labels;

            // Each label is presented in a grid with background color and text.
            foreach (var label in labels)
            {
                var grid = new Grid
                {
                    Background = GetBrushFromColor(label.Item2, 0.4),
                    Padding = new Thickness(7, 2, 7, 2),
                };
                if (labelGroup.RoundedCorners)
                {
                    grid.CornerRadius = new CornerRadius(7);
                }

                var tb = new TextBlock
                {
                    Text = label.Item1,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                };
                grid.Children.Add(tb);
                wrapPanel.Children.Add(grid);
            }
        }

        return wrapPanel;
    }

    /// <summary>
    /// Create a SolidColorBrush from the given RGB color and opacity. If a color is not supplied, return a transparent brush.
    /// </summary>
    /// <param name="colorString">A string in 6-character RGB format.</param>
    /// <param name="opacity">A percentage given as a double between 0 and 1.</param>
    private Microsoft.UI.Xaml.Media.SolidColorBrush GetBrushFromColor(string colorString, double opacity)
    {
        if (!string.IsNullOrEmpty(colorString))
        {
            var a = (byte)System.Convert.ToUInt32(255 * opacity);
            var r = (byte)System.Convert.ToUInt32(colorString[..2], 16);
            var g = (byte)System.Convert.ToUInt32(colorString.Substring(2, 2), 16);
            var b = (byte)System.Convert.ToUInt32(colorString.Substring(4, 2), 16);

            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
}
