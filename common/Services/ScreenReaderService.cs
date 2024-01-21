// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.Common.Services;

public class ScreenReaderService : IScreenReaderService
{
    /// <inheritdoc/>
    public event EventHandler<string>? AnnouncementTextChanged;

    /// <inheritdoc/>
    public void Announce(string text) => AnnouncementTextChanged?.Invoke(null, text);
}
