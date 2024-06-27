// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Common.Environments.Exceptions;

public class CreateCreateComputeSystemOperationException : Exception
{
    public CreateCreateComputeSystemOperationException(string message)
        : base(message)
    {
    }
}
