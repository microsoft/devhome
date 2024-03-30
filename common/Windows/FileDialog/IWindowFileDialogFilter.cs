// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Common.Windows.FileDialog;

/// <summary>
/// Represents a filter for a file dialog.
/// </summary>
public interface IWindowFileDialogFilter
{
    public string? Name { get; }

    public string? Spec { get; }

    public IReadOnlyList<string> Patterns { get; }
}
