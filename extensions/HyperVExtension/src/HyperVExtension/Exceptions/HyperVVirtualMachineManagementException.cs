// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class HyperVVirtualMachineManagementException : HyperVManagerException
{
    public HyperVVirtualMachineManagementException(string message)
        : base(message)
    {
    }
}
