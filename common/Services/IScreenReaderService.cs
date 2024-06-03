// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Common.Services;

public interface IScreenReaderService
{
    /// <summary>
    /// Occurs when announcing a text
    /// </summary>
    public event EventHandler<string>? AnnouncementTextChanged;

    /// <summary>
    /// Announce the provided text
    /// </summary>
    /// <param name="text">Text to announce by the screen reader</param>
    public void Announce(string text);
}
