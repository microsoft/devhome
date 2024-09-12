// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.TelemetryEvents;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the out of proc ICreateComputeSystemOperation interface that is received from the extension.
/// </summary>
public class CreateComputeSystemOperation : IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CreateComputeSystemOperation));

    private readonly Guid _activityId;

    // These operations are stored by the ComputeSystemManager who can then provide them to the environments
    // page to be displayed to the user.
    public Guid OperationId { get; } = Guid.NewGuid();

    /// <summary>
    /// The original ICreateComputeSystemOperation object that was received from the extension.
    /// </summary>
    private readonly ICreateComputeSystemOperation _createComputeSystemOperation;

    /// <summary>
    /// Wrapper for the <see cref="ICreateComputeSystemOperation.ActionRequired"/> that is received from the extension."/>
    /// </summary>
    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired;

    /// <summary>
    /// Wrapper for the <see cref="ICreateComputeSystemOperation.Progress"/> that is received from the extension."/>
    /// </summary>
    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;

    /// <summary>
    /// This is not an extension event, it provides a way for another object receive the result of the operation.
    /// </summary>
    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemResult>? Completed;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public string EnvironmentName { get; private set; } = StringResourceHelper.GetResource("EnvironmentGenericName");

    public ComputeSystemProviderDetails ProviderDetails { get; }

    private readonly Dictionary<string, string> _userInputJsonMap;

    /// <summary>
    /// Gets the last progress message that was received from the extension. This is useful if we initialing missed the progress message.
    /// </summary>
    public string LastProgressMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the last progress message that was received from the extension. This is useful if we initialing missed the progress percentage.
    /// </summary>
    public uint LastProgressPercentage { get; private set; }

    public CreateComputeSystemResult? CreateComputeSystemResult { get; private set; }

    /// <summary>
    /// Since we don't actually know what the user input json will look like, we check for the known key name 'NewEnvironmentName'.
    /// This "known" key will be provided as guidance to extension creators. However, its really up to the extension to decide what key they want to use.
    /// Since this input is coming from an adaptive card from the extension we know we'll only get string key/values pairs. So we expect to see a
    /// key/value pair like this:
    ///
    ///    {"NewEnvironmentName" : "MyNewEnvironment"}
    ///
    /// In the future we can update the ICreateComputeSystemOperation interface in the SDK to have metadata about the new environment.
    /// This way we can have strongly typed access to what the new environment name is, as well as other properties we expect an environment to have.
    /// </summary>
    private const string EnvironmentNameJsonKey = "NewEnvironmentName";

    private bool _disposedValue;

    public CreateComputeSystemOperation(
        ICreateComputeSystemOperation createComputeSystemOperation,
        ComputeSystemProviderDetails providerDetails,
        string userInputJson,
        Guid activityId)
    {
        _createComputeSystemOperation = createComputeSystemOperation;
        ProviderDetails = providerDetails;
        _createComputeSystemOperation.ActionRequired += OnActionRequired;
        _createComputeSystemOperation.Progress += OnProgress;

        _userInputJsonMap = JsonSerializer.Deserialize<Dictionary<string, string>>(userInputJson) ?? new();

        // Try to find the environment name in the user input json. This is the Id of the adaptive card element that allowed the user to enter
        // their environment name. If the key is not found, we'll just use the generic name.
        foreach (var key in _userInputJsonMap.Keys)
        {
            if (key.Equals(EnvironmentNameJsonKey, StringComparison.OrdinalIgnoreCase))
            {
                EnvironmentName = _userInputJsonMap[key];
                break;
            }
        }

        _activityId = activityId;
    }

    public void StartOperation()
    {
        // Fire the task on a background thread so that the UI thread is not blocked.
        Task.Run(async () =>
        {
            try
            {
                CreateComputeSystemResult = await _createComputeSystemOperation.StartAsync().AsTask(_cancellationTokenSource.Token);
                Completed?.Invoke(this, CreateComputeSystemResult);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"StartOperation failed for provider {ProviderDetails.ComputeSystemProvider}");
                CreateComputeSystemResult = new CreateComputeSystemResult(ex, StringResourceHelper.GetResource("CreationOperationStoppedUnexpectedly"), ex.Message);
                Completed?.Invoke(this, CreateComputeSystemResult);
            }

            var (_, _, telemetryStatus) = ComputeSystemHelpers.LogResult(CreateComputeSystemResult?.Result, _log);
            var telemetryResult = new TelemetryResult(CreateComputeSystemResult?.Result);
            var providerId = ProviderDetails.ComputeSystemProvider.Id;

            TelemetryFactory.Get<ITelemetry>().Log(
                "Environment_Creation_Event",
                LogLevel.Critical,
                new EnvironmentCreationEvent(ProviderDetails.ComputeSystemProvider.Id, telemetryStatus, telemetryResult),
                _activityId);

            RemoveEventHandlers();
        });
    }

    private void OnActionRequired(ICreateComputeSystemOperation sender, CreateComputeSystemActionRequiredEventArgs args)
    {
        ActionRequired?.Invoke(this, args);
    }

    private void OnProgress(ICreateComputeSystemOperation sender, CreateComputeSystemProgressEventArgs args)
    {
        // This object may not appear in th UI, immediately so in case there were progress updates before the UI is ready, we store the last progress message and percentage.
        // to be used when the UI is ready.
        LastProgressMessage = args.Status;
        LastProgressPercentage = args.PercentageCompleted;

        Progress?.Invoke(this, args);
    }

    public void RemoveEventHandlers()
    {
        try
        {
            _createComputeSystemOperation.ActionRequired -= OnActionRequired;
            _createComputeSystemOperation.Progress -= OnProgress;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to remove event handlers for {this}");
        }
    }

    public void CancelOperation()
    {
        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to cancel operation for {this}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _cancellationTokenSource.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
