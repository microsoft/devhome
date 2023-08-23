// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DevHome.Dashboard.Controls;

/// <summary>
/// Arranges child elements into a staggered grid pattern where items are added to the column that has used least amount of space.
/// </summary>
public sealed class WidgetBoard : Panel
{
    private double _columnWidth;
    private const int _maxColumns = 4;

    // We need to control the HorizontalAlignment, so hide this property.
#pragma warning disable SA1306 // Field names should begin with lower-case letter
    private new HorizontalAlignment HorizontalAlignment;
#pragma warning restore SA1306 // Field names should begin with lower-case letter

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetBoard"/> class.
    /// </summary>
    /// <remarks>
    /// This control is based off of the
    /// <see href="https://learn.microsoft.com/dotnet/api/microsoft.toolkit.uwp.ui.controls.staggeredpanel">StaggeredPanel</see>
    /// control from the Windows Community Toolkit.
    /// </remarks>
    public WidgetBoard()
    {
        RegisterPropertyChangedCallback(Panel.HorizontalAlignmentProperty, OnHorizontalAlignmentChanged);
        HorizontalAlignment = HorizontalAlignment.Left;
    }

    /// <summary>
    /// Gets or sets the width for each Widget. All widgets have the same width.
    /// </summary>
    public double WidgetWidth
    {
        get => (double)GetValue(WidgetWidthProperty);
        set => SetValue(WidgetWidthProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="WidgetWidth"/> dependency property. The default value is 300.
    /// </summary>
    /// <returns>The identifier for the <see cref="WidgetWidth"/> dependency property.</returns>
    public static readonly DependencyProperty WidgetWidthProperty = DependencyProperty.Register(
        nameof(WidgetWidth),
        typeof(double),
        typeof(WidgetBoard),
        new PropertyMetadata(300d, OnWidgetWidthChanged));

    /// <summary>
    /// Gets or sets the distance between the border and its child object.
    /// </summary>
    /// <returns>
    /// The dimensions of the space between the border and its child as a Thickness value.
    /// Thickness is a structure that stores dimension values using pixel measures.
    /// </returns>
    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// Identifies the Padding dependency property.
    /// </summary>
    /// <returns>The identifier for the <see cref="Padding"/> dependency property.</returns>
    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
        nameof(Padding),
        typeof(Thickness),
        typeof(WidgetBoard),
        new PropertyMetadata(default(Thickness), OnPaddingChanged));

    /// <summary>
    /// Gets or sets the spacing between columns of widgets. The default value is 12.
    /// </summary>
    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ColumnSpacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register(
        nameof(ColumnSpacing),
        typeof(double),
        typeof(WidgetBoard),
        new PropertyMetadata(12d, OnPaddingChanged));

    /// <summary>
    /// Gets or sets the spacing between rows of widgets. The default value is 12.
    /// </summary>
    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="RowSpacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.Register(
        nameof(RowSpacing),
        typeof(double),
        typeof(WidgetBoard),
        new PropertyMetadata(12d, OnPaddingChanged));

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = availableSize.Width - Padding.Left - Padding.Right;
        var availableHeight = availableSize.Height - Padding.Top - Padding.Bottom;

        _columnWidth = Math.Min(WidgetWidth, availableWidth);
        var numColumns = Math.Max(1, (int)Math.Floor(availableWidth / _columnWidth));
        numColumns = numColumns > _maxColumns ? _maxColumns : numColumns;

        // When only one widget is pinned, it should be centered. Otherwise, left align.
        // Set the number of columns to 1 so it is properly measured and centered.
        if (Children.Count == 1)
        {
            HorizontalAlignment = HorizontalAlignment.Center;
            numColumns = 1;
        }
        else
        {
            HorizontalAlignment = HorizontalAlignment.Left;
        }

        // Adjust for column spacing on all columns except the first.
        var totalWidth = _columnWidth + ((numColumns - 1) * (_columnWidth + ColumnSpacing));
        if (totalWidth > availableWidth)
        {
            numColumns--;
        }
        else if (double.IsInfinity(availableWidth))
        {
            availableWidth = totalWidth;
        }

        if (Children.Count == 0)
        {
            return new Size(0, 0);
        }

        var columnHeights = new double[numColumns];
        var itemsPerColumn = new double[numColumns];

        for (var i = 0; i < Children.Count; i++)
        {
            var columnIndex = GetColumnIndex(columnHeights);

            var child = Children[i];
            child.Measure(new Size(_columnWidth, availableHeight));
            var elementSize = child.DesiredSize;
            columnHeights[columnIndex] += elementSize.Height + (itemsPerColumn[columnIndex] > 0 ? RowSpacing : 0);
            itemsPerColumn[columnIndex]++;
        }

        var desiredHeight = columnHeights.Max();

        return new Size(availableWidth, desiredHeight);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontalOffset = Padding.Left;
        var verticalOffset = Padding.Top;

        var numColumns = Math.Max(1, (int)Math.Floor(finalSize.Width / _columnWidth));
        numColumns = numColumns > _maxColumns ? _maxColumns : numColumns;

        // When only one widget is pinned, it should be centered. (Otherwise, left align.)
        // Set the number of columns to 1 so it is properly measured and centered.
        if (Children.Count == 1)
        {
            numColumns = 1;
        }

        // Adjust for horizontal spacing on all columns expect the first.
        var totalWidth = _columnWidth + ((numColumns - 1) * (_columnWidth + ColumnSpacing));
        if (totalWidth > finalSize.Width)
        {
            numColumns--;

            // Need to recalculate the totalWidth for a correct horizontal offset.
            totalWidth = _columnWidth + ((numColumns - 1) * (_columnWidth + ColumnSpacing));
        }

        // HorizontalAlignment should not be Right aligned, but include logic here in case we enable it in the future.
        if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            horizontalOffset += finalSize.Width - totalWidth;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            horizontalOffset += (finalSize.Width - totalWidth) / 2;
        }

        var columnHeights = new double[numColumns];
        var itemsPerColumn = new double[numColumns];

        for (var i = 0; i < Children.Count; i++)
        {
            var columnIndex = GetColumnIndex(columnHeights);

            var child = Children[i];
            var elementSize = child.DesiredSize;

            var elementHeight = elementSize.Height;

            var itemHorizontalOffset = horizontalOffset + (_columnWidth * columnIndex) + (ColumnSpacing * columnIndex);
            var itemVerticalOffset = columnHeights[columnIndex] + verticalOffset + (RowSpacing * itemsPerColumn[columnIndex]);

            var bounds = new Rect(itemHorizontalOffset, itemVerticalOffset, _columnWidth, elementHeight);
            child.Arrange(bounds);

            columnHeights[columnIndex] += elementSize.Height;
            itemsPerColumn[columnIndex]++;
        }

        return base.ArrangeOverride(finalSize);
    }

    private static void OnWidgetWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (WidgetBoard)d;
        panel.InvalidateMeasure();
    }

    private static void OnPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (WidgetBoard)d;
        panel.InvalidateMeasure();
    }

    private void OnHorizontalAlignmentChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (HorizontalAlignment == HorizontalAlignment.Stretch || HorizontalAlignment == HorizontalAlignment.Right)
        {
            throw new NotSupportedException();
        }

        InvalidateMeasure();
    }

    private int GetColumnIndex(double[] columnHeights)
    {
        var columnIndex = 0;
        var height = columnHeights[0];
        for (var j = 1; j < columnHeights.Length; j++)
        {
            if (columnHeights[j] < height)
            {
                columnIndex = j;
                height = columnHeights[j];
            }
        }

        return columnIndex;
    }
}
