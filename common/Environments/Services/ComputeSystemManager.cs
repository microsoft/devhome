// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace DevHome.Common.Environments.Services;

/// <summary>
/// Service thats used to get the ComputeSystems from the providers so they can be loaded into the UI.
/// This class is also used to keep track of the ComputeSystem that a configuration file will be applied to.
/// </summary>
public class ComputeSystemManager : IComputeSystemManager
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemManager));

    private readonly IComputeSystemService _computeSystemService;

    private readonly Dictionary<Guid, CreateComputeSystemOperation> _createComputeSystemOperations = new();

    public event TypedEventHandler<ComputeSystem, ComputeSystemState> ComputeSystemStateChanged = (sender, state) => { };

    private readonly object _creationOperationLock = new();

    // Used in the setup flow to store the ComputeSystem needed to configure.
    public ComputeSystemReviewItem? ComputeSystemSetupItem { get; set; }

    public ComputeSystemManager(IComputeSystemService computeSystemService)
    {
        _computeSystemService = computeSystemService;
    }

    /// <summary>
    /// This method gets the ComputeSystems from the providers in parallel.
    /// </summary>
    public async Task GetComputeSystemsAsync(Func<ComputeSystemsLoadedData, Task> callback)
    {
        // Create a cancellation token that will cancel the task after 2 minute.
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(2));
        var token = cancellationTokenSource.Token;
        var computeSystemsProviderDetails = await _computeSystemService.GetComputeSystemProvidersAsync();

        try
        {
            // get compute systems from providers in parallel.
            await Parallel.ForEachAsync(computeSystemsProviderDetails, async (providerDetails, token) =>
            {
                var provider = providerDetails.ComputeSystemProvider;
                var devIdWrappers = new List<DeveloperIdWrapper>();
                var results = new List<ComputeSystemsResult>();
                var wrapperDictionary = new Dictionary<DeveloperIdWrapper, ComputeSystemsResult>();

                foreach (var devIdWrapper in providerDetails.DeveloperIds)
                {
                    var result = await providerDetails.ComputeSystemProvider.GetComputeSystemsAsync(devIdWrapper.DeveloperId);
                    wrapperDictionary.Add(devIdWrapper, result);
                    results.Add(result);
                }

                var loadedData = new ComputeSystemsLoadedData(providerDetails, wrapperDictionary);
                await callback(loadedData);
            });
        }
        catch (AggregateException aggregateEx)
        {
            foreach (var innerEx in aggregateEx.InnerExceptions)
            {
                if (innerEx is TaskCanceledException)
                {
                    _log.Error(innerEx, $"Failed to get retrieve all compute systems from all compute system providers due to cancellation");
                }
                else
                {
                    _log.Error(innerEx, $"Failed to get retrieve all compute systems from all compute system providers ");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get retrieve all compute systems from all compute system providers ");
        }
    }

    public void OnComputeSystemStateChanged(ComputeSystem sender, ComputeSystemState state)
    {
        ComputeSystemStateChanged(sender, state);
    }

    public List<CreateComputeSystemOperation> GetRunningOperationsForCreation()
    {
        lock (_creationOperationLock)
        {
            return _createComputeSystemOperations.Values.ToList();
        }
    }

    public void AddRunningOperationForCreation(CreateComputeSystemOperation operation)
    {
        lock (_creationOperationLock)
        {
            _createComputeSystemOperations.Add(operation.OperationId, operation);
        }
    }

    public void RemoveOperation(CreateComputeSystemOperation operation)
    {
        lock (_creationOperationLock)
        {
            _createComputeSystemOperations.Remove(operation.OperationId);
        }
    }

    public void RemoveAllCompletedOperations()
    {
        lock (_creationOperationLock)
        {
            var totalOperations = _createComputeSystemOperations.Count;
            for (var i = 0; i < totalOperations; i++)
            {
                var operation = _createComputeSystemOperations.ElementAt(i).Value;
                if (operation.CreateComputeSystemResult != null)
                {
                    _createComputeSystemOperations.Remove(operation.OperationId);
                }
            }
        }
    }
}
