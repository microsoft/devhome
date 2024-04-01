// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Environments.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the out of proc ICreateComputeSystemOperation interface that is receieved from the extension.
/// </summary>
public class CreateComputeSystemOperation
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CreateComputeSystemOperation));

    // These operations are stored by the ComputeSystemManager who can then provide them to the environments
    // page to be displayed to the user.
    public Guid OperationId { get; } = Guid.NewGuid();

    private readonly ICreateComputeSystemOperation _createComputeSystemOperation;

    private readonly string _environmentGenericName = StringResourceHelper.GetResource("EnvironmentGenericName");

    public string EnvironmentName { get; private set; }

    private readonly Dictionary<string, string> _userInputJsonMap;

    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public string LastProgressMessage { get; private set; } = string.Empty;

    public uint LastProgressPercentage { get; private set; }

    public CreateComputeSystemResult? CreateComputeSystemResult { get; private set; }

    // Since we don't actually know what the user input json will look like, we check for known key names that possibly contain the environment name.
    // These "known" keys are names that could be used but its really up to the extension to decide what key they want to use. We can provide guidance
    // on what key to use but we can't strongly enforce it, since this input is coming from an adaptive card from the extension. In the future we can
    // update the ICreateComputeSystemOperation interface in the SDK to have metadata about the new environment. This way we can have strongly typed access
    // to what the new environment name is, as well as other properties we expect an environment to have.
    private static readonly HashSet<string> MapOfKnownEnvironmentNameJsonKeys = new()
    {
        "NewVirtualMachineName",
        "VirtualMachineName",
        "NewEnvironmentName",
        "EnvironmentName",
    };

    public CreateComputeSystemOperation(ICreateComputeSystemOperation createComputeSystemOperation, ComputeSystemProviderDetails providerDetails, string userInputJson)
    {
        _createComputeSystemOperation = createComputeSystemOperation;
        _createComputeSystemOperation.ActionRequired += OnActionRequired;
        _createComputeSystemOperation.Progress += OnProgress;

        _userInputJsonMap = JsonSerializer.Deserialize<Dictionary<string, string>>(userInputJson) ?? new();
        ProviderDetails = providerDetails;
        EnvironmentName = _environmentGenericName;

        foreach (var key in _userInputJsonMap.Keys)
        {
            if (MapOfKnownEnvironmentNameJsonKeys.Contains(key))
            {
                EnvironmentName = _userInputJsonMap[key];
                break;
            }
        }
    }

    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired;

    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;

    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemResult>? Completed;

    public void StartOperation(CancellationToken cancellationToken)
    {
        Task.Run(
            async () =>
            {
                try
                {
                    CreateComputeSystemResult = await _createComputeSystemOperation.StartAsync().AsTask(cancellationToken);
                    Completed?.Invoke(this, CreateComputeSystemResult);
                }
                catch (Exception ex)
                {
                    _log.Error($"CreateComputeSystemOperation failed for provider {ProviderDetails.ComputeSystemProvider}", ex);
                    CreateComputeSystemResult = new CreateComputeSystemResult(ex, StringResourceHelper.GetResource("CreationOperationStoppedUnexpectedly"), ex.Message);
                    Completed?.Invoke(this, CreateComputeSystemResult);
                }
            },
            CancellationToken.None);
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
            _log.Error($"Failed to remove event handlers for {this}", ex);
        }
    }
}
