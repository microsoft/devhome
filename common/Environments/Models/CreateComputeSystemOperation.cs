// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using DevHome.SetupFlow.Models.Environments;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for ICreateComputeSystemOperation COM object we get from the extension.
/// </summary>
public class CreateComputeSystemOperation
{
    private readonly string _componentName = "CreateComputeSystemOperation";

    private readonly ICreateComputeSystemOperation _createComputeSystemOperation;

    private readonly string _errorText = StringResourceHelper.GetResource("CreateComputeSystemOperationError");

    public CreateComputeSystemOperation(ICreateComputeSystemOperation createComputeSystemOperation)
    {
        _createComputeSystemOperation = createComputeSystemOperation;
        WeakReferenceMessenger.Default.Send(new CreationOperationReceivedMessage(this));
        _createComputeSystemOperation.ActionRequired += OnActionRequired;
        _createComputeSystemOperation.Progress += OnProgressReported;
    }

    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired;

    public event TypedEventHandler<CreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;

    public async Task<CreateComputeSystemResult> StartAsync(CancellationToken cancellationToken)
    {
        CreateComputeSystemResult result;
        try
        {
            result = await _createComputeSystemOperation.StartAsync().AsTask(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"StartAsync for failed due to exception", ex);

            result = new CreateComputeSystemResult(ex, _errorText, ex.Message);
        }

        _createComputeSystemOperation.ActionRequired -= OnActionRequired;
        _createComputeSystemOperation.Progress -= OnProgressReported;
        WeakReferenceMessenger.Default.Send(new CreationOperationEndedMessage(this));
        return result;
    }

    public void OnActionRequired(ICreateComputeSystemOperation sender, CreateComputeSystemActionRequiredEventArgs args)
    {
        ActionRequired?.Invoke(this, args);
    }

    public void OnProgressReported(ICreateComputeSystemOperation sender, CreateComputeSystemProgressEventArgs args)
    {
        Progress?.Invoke(this, args);
    }
}
