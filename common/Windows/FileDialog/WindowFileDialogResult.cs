// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Windows.FileDialog;

public class WindowFileDialogResult
{
    public bool IsCanceled { get; init; }

    public string? FileName { get; init; }

    public WindowFileDialogFilter? FileType { get; init; }
}
