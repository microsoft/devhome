// Copyright(c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;

namespace DevHome.Common.Environments.Services;
public interface IComputeSystemManager
{
    /// <summary>
    /// Gets or sets the compute system that a configuration file will be applied to.
    /// </summary>
    public ComputeSystemReviewItem? ComputeSystemSetupItem { get; set; }

    public Task GetComputeSystemsAsync(Func<ComputeSystemsLoadedData, Task> callback);
}
