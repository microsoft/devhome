// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
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

    public Task GetComputeSystemsAsync(Func<ComputeSystemsLoadedData, Task> callback, CancellationToken cancellationToken);

    public event TypedEventHandler<ComputeSystem, ComputeSystemState> ComputeSystemStateChanged;

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state);

    public List<CreateComputeSystemOperation> GetRunningOperationsForCreation();

    public void AddRunningOperationForCreation(CreateComputeSystemOperation operation);

    public void RemoveOperation(CreateComputeSystemOperation operation);

    public void RemoveAllCompletedOperations();
}
