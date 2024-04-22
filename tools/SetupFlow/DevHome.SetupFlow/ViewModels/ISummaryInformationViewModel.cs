// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.ViewModels;

public interface ISummaryInformationViewModel
{
    /// <summary>
    /// Gets a value indicating whether this object has enough data to be used
    /// in the next steps portion of the summary screen.
    /// </summary>
    public bool HasContent { get; }
}
