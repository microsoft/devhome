// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Windows.FileDialog;

/// <summary>
/// Represents a filter for a file dialog.
/// </summary>
public interface IWindowFileDialogFilter
{
    /// <summary>
    /// Gets the display name of the filter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the filter specified pattern.
    /// </summary>
    /// <remarks>This contains the combined pattern of all the patterns in the filter.</remarks>
    public string Spec { get; }

    /// <summary>
    /// Gets the list of atomic patterns that make up the filter.
    /// </summary>
    public IReadOnlyList<string> Patterns { get; }
}
