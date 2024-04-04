// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Class that represents a failed create compute system operation. We use this when we were unable to get the original
/// ICreateComputeSystemOperation object from the extension. E.g a COM exception was thrown.
/// </summary>
public class FailedCreateComputeSystemOperation : ICreateComputeSystemOperation
{
    public FailedCreateComputeSystemOperation(Exception exception, string localizedErrorMessage)
    {
        LocalizedErrorMessage = localizedErrorMessage;
        Exception = exception;
    }

    public string LocalizedErrorMessage { get; }

    public Exception Exception { get; }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs> Progress = (s, e) => { };

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs> ActionRequired = (s, e) => { };

    public IAsyncOperation<CreateComputeSystemResult> StartAsync()
    {
        return Task.FromResult(new CreateComputeSystemResult(Exception, LocalizedErrorMessage, Exception.Message)).AsAsyncOperation();
    }
}
