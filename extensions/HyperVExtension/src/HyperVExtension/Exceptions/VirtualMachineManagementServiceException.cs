// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class VirtualMachineManagementServiceException : Exception
{
    public VirtualMachineManagementServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
