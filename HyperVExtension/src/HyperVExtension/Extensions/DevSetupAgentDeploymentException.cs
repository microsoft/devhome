// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class DevSetupAgentDeploymentException : Exception
{
    public DevSetupAgentDeploymentException(string? message)
        : base(message)
    {
    }

    public DevSetupAgentDeploymentException(string? message, Exception? innerException)
    : base(message, innerException)
    {
    }
}
