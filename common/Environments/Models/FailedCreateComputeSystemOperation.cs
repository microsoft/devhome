// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

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
