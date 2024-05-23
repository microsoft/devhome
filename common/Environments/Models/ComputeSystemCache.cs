// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the IComputeSystem interface that can be used throughout the application.
/// It uses lazy initialization to cache the results of OOP calls to the extension methods.
/// The implementation of the calls to IComputeSystem remains in ComputeSystem class for now.
/// Once we remove direct usage of ComputeSystem class throughout the code, we can merge these two classes.
/// </summary>
public class ComputeSystemCache
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemCache));

    public ComputeSystem ComputeSystem { get; private set; }

    public Lazy<string?> Id { get; private set; }

    public Lazy<string> DisplayName { get; private set; }

    public Lazy<ComputeSystemOperations> SupportedOperations { get; private set; }

    public Lazy<string> SupplementalDisplayName { get; private set; }

    public Lazy<IDeveloperId> AssociatedDeveloperId { get; private set; }

    public Lazy<string> AssociatedProviderId { get; private set; }

    private readonly Lazy<Task<ComputeSystemStateResult>> _stateResult;
    private readonly Lazy<Task<ComputeSystemThumbnailResult>> _thumbnailResult;
    private Lazy<Task<IEnumerable<ComputeSystemPropertyCache>>> _properties;
    private Lazy<Task<ComputeSystemPinnedResult>> _pinnedToStartResult;
    private Lazy<Task<ComputeSystemPinnedResult>> _pinnedToTaskbarResult;

    // This is used to store the parameter for the thumbnail request so that it can be used with lazy initialization.
    // There is a race if it's used concurrently from different threads, however it's not expected to use this class
    // to initialize properties concurrently and these parameters are not used at the moment.
    private string _thumbnailParameter = string.Empty;
    private string _propertiesParameter = string.Empty;

    public ComputeSystemCache(ComputeSystem computeSystem)
    {
        ComputeSystem = computeSystem;

        // TODO: The following non-async properties are already cached in ComputeSystem. Once we update
        // code to use ComputeSystemCache in most places, we can do all the caching in this class.
        Id = new Lazy<string?>(() => ComputeSystem.Id);
        DisplayName = new Lazy<string>(() => ComputeSystem.DisplayName);
        SupplementalDisplayName = new Lazy<string>(() => ComputeSystem.SupplementalDisplayName);
        AssociatedDeveloperId = new Lazy<IDeveloperId>(() => ComputeSystem.AssociatedDeveloperId);
        AssociatedProviderId = new Lazy<string>(() => ComputeSystem.AssociatedProviderId);
        SupportedOperations = new Lazy<ComputeSystemOperations>(() => ComputeSystem.SupportedOperations);

        // Async properties
        _stateResult = new Lazy<Task<ComputeSystemStateResult>>(() => ComputeSystem.GetStateAsync());
        _thumbnailResult = new Lazy<Task<ComputeSystemThumbnailResult>>(() => ComputeSystem.GetComputeSystemThumbnailAsync(_thumbnailParameter));
        _properties = new Lazy<Task<IEnumerable<ComputeSystemPropertyCache>>>(() => InitComputeSystemPropertiesAsync(_propertiesParameter));
        _pinnedToStartResult = new Lazy<Task<ComputeSystemPinnedResult>>(() => ComputeSystem.GetIsPinnedToStartMenuAsync());
        _pinnedToTaskbarResult = new Lazy<Task<ComputeSystemPinnedResult>>(() => ComputeSystem.GetIsPinnedToTaskbarAsync());
    }

    public ComputeSystemCache(IComputeSystem computeSystem)
        : this(new ComputeSystem(computeSystem))
    {
    }

    public event TypedEventHandler<ComputeSystem, ComputeSystemState> StateChanged
    {
        add => ComputeSystem.StateChanged += value;
        remove => ComputeSystem.StateChanged -= value;
    }

    public async Task<ComputeSystemStateResult> GetStateAsync()
    {
        return await _stateResult.Value;
    }

    public async Task<ComputeSystemOperationResult> StartAsync(string options)
    {
        return await ComputeSystem.StartAsync(options);
    }

    public async Task<ComputeSystemOperationResult> ShutDownAsync(string options)
    {
        return await ComputeSystem.ShutDownAsync(options);
    }

    public async Task<ComputeSystemOperationResult> RestartAsync(string options)
    {
        return await ComputeSystem.RestartAsync(options);
    }

    public async Task<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return await ComputeSystem.TerminateAsync(options);
    }

    public async Task<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        return await ComputeSystem.DeleteAsync(options);
    }

    public async Task<ComputeSystemOperationResult> SaveAsync(string options)
    {
        return await ComputeSystem.SaveAsync(options);
    }

    public async Task<ComputeSystemOperationResult> PauseAsync(string options)
    {
        return await ComputeSystem.PauseAsync(options);
    }

    public async Task<ComputeSystemOperationResult> ResumeAsync(string options)
    {
        return await ComputeSystem.ResumeAsync(options);
    }

    public async Task<ComputeSystemOperationResult> CreateSnapshotAsync(string options)
    {
        return await ComputeSystem.CreateSnapshotAsync(options);
    }

    public async Task<ComputeSystemOperationResult> RevertSnapshotAsync(string options)
    {
        return await ComputeSystem.RevertSnapshotAsync(options);
    }

    public async Task<ComputeSystemOperationResult> DeleteSnapshotAsync(string options)
    {
        return await ComputeSystem.DeleteSnapshotAsync(options);
    }

    public async Task<ComputeSystemOperationResult> ModifyPropertiesAsync(string options)
    {
        return await ComputeSystem.ModifyPropertiesAsync(options);
    }

    public async Task<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        _thumbnailParameter = options;
        return await _thumbnailResult.Value;
    }

    public async Task<IEnumerable<ComputeSystemPropertyCache>> InitComputeSystemPropertiesAsync(string options)
    {
        var remoteProperties = await ComputeSystem.GetComputeSystemPropertiesAsync(options);
        var localProperties = new List<ComputeSystemPropertyCache>();
        foreach (var property in remoteProperties)
        {
            try
            {
                localProperties.Add(new ComputeSystemPropertyCache(property));
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to get property value for {ComputeSystem} due to exception");
            }
        }

        return localProperties;
    }

    public async Task<IEnumerable<ComputeSystemPropertyCache>> GetComputeSystemPropertiesAsync(string options)
    {
        _propertiesParameter = options;
        return await _properties.Value;
    }

    public async Task<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return await ComputeSystem.ConnectAsync(options);
    }

    public async Task<ComputeSystemOperationResult> PinToStartMenuAsync(string options)
    {
        return await ComputeSystem.PinToStartMenuAsync(options);
    }

    public async Task<ComputeSystemOperationResult> UnpinFromStartMenuAsync(string options)
    {
        return await ComputeSystem.UnpinFromStartMenuAsync(options);
    }

    public async Task<ComputeSystemOperationResult> PinToTaskbarAsync(string options)
    {
        return await ComputeSystem.PinToTaskbarAsync(options);
    }

    public async Task<ComputeSystemOperationResult> UnpinFromTaskbarAsync(string options)
    {
        return await ComputeSystem.UnpinFromTaskbarAsync(options);
    }

    public async Task<ComputeSystemPinnedResult> GetIsPinnedToStartMenuAsync()
    {
        return await _pinnedToStartResult.Value;
    }

    public void ResetPinnedToStartMenu()
    {
        _pinnedToStartResult = new Lazy<Task<ComputeSystemPinnedResult>>(() => ComputeSystem.GetIsPinnedToStartMenuAsync());
    }

    public async Task<ComputeSystemPinnedResult> GetIsPinnedToTaskbarAsync()
    {
        return await _pinnedToTaskbarResult.Value;
    }

    public void ResetPinnedToTaskbar()
    {
        _pinnedToTaskbarResult = new Lazy<Task<ComputeSystemPinnedResult>>(() => ComputeSystem.GetIsPinnedToTaskbarAsync());
    }

    public void ResetComputeSystemProperties()
    {
        _properties = new Lazy<Task<IEnumerable<ComputeSystemPropertyCache>>>(() => InitComputeSystemPropertiesAsync(_propertiesParameter));
    }

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration)
    {
        return ComputeSystem.CreateApplyConfigurationOperation(configuration);
    }

    public async Task FetchDataAsync()
    {
        _ = await GetStateAsync();
        var supportedOperations = SupportedOperations?.Value ?? ComputeSystemOperations.None;

        var s2 = DateTime.Now;
        _ = await GetComputeSystemThumbnailAsync(string.Empty);
        _ = await GetComputeSystemPropertiesAsync(string.Empty);
    }

    public void ResetSupportedOperations()
    {
        SupportedOperations = new Lazy<ComputeSystemOperations>(() => ComputeSystem.SupportedOperations);
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem ID: {Id} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem name: {DisplayName} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem SupplementalDisplayName: {SupplementalDisplayName} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem associated Provider Id : {AssociatedProviderId} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem associated developerId LoginId: {AssociatedDeveloperId?.Value?.LoginId} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem associated developerId Url: {AssociatedDeveloperId?.Value?.Url} ");

        var supportedOperations = EnumHelper.SupportedOperationsToString<ComputeSystemOperations>(SupportedOperations.Value);
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem supported operations : {string.Join(",", supportedOperations)} ");

        return builder.ToString();
    }
}
