// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
    /// Gets or sets the title for the property.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Gets or sets an object that contains both light and dark icons to be used by Dev Home, for property icons provided by the compute system extensions.
    /// </summary>
    /// public ExtensionIcon ExtensionProvidedIcon { get; set; }
}
