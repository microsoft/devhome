// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Common.Environments.Services;

public interface IComputeSystemManager
{
    /// <summary>
    /// Gets or sets the compute system that a configuration file will be applied to.
    /// </summary>
    public ComputeSystemReviewItem? ComputeSystemSetupItem { get; set; }

    public Task GetComputeSystemsAsync(Func<ComputeSystemsLoadedData, Task> callback);

    public event TypedEventHandler<ComputeSystem, ComputeSystemState> ComputeSystemStateChanged;

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state);
}
