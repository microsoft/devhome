// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Enum value that represent additional non compute system specific actions that can be taken by
/// the user from Dev Home.
/// </summary>
public enum EnvironmentAdditionalActions
{
    PinToStart,
    PinToTaskBar,
}

/// <summary>
/// Enum values that are used to visually represent the state of a compute system in the UI.
/// </summary>
public enum CardStateColor
{
    Success,
    Neutral,
    Caution,
}

/// <summary>
/// base interface for all properties that will appear in the Environment cards.
/// </summary>
public interface ICardProperty
{
    /// <summary>
    /// Gets the title for the property.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets a bitmap image that was created from a Uri which contains the path to a icon resource in an extension packages .pri file.
    /// </summary>
    public BitmapImage? Icon { get; }

    /// <summary>
    /// Gets glyphs in Segoe Fluent icons to be shown in the UI.
    public string? Glyph { get; }
}
