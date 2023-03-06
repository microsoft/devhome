// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.SetupFlow.Common.Models;

/// <summary>
/// Enum to tell Dev Home the status of a task.
/// </summary>
public enum TaskFinishedState
{
    Success,
    Failure,
    NeedsAttention,
}
