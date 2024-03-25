// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using DevHome.Common.Helpers;
using Windows.Foundation;
using DevHome.Common.Environments.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.SetupFlow.Models.Environments;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for ICreateComputeSystemOperation.
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
