// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.Exceptions;

public class HyperVVirtualMachineManagementException : HyperVManagerException
{
    public HyperVVirtualMachineManagementException(string message)
        : base(message)
    {
    }
}
