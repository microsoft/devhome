// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class DevSetupAgentDeploymentSessionException : Exception
{
    public DevSetupAgentDeploymentSessionException(string? message)
        : base(message)
    {
    }

    public DevSetupAgentDeploymentSessionException(string? message, Exception? innerException)
    : base(message, innerException)
    {
    }
}
