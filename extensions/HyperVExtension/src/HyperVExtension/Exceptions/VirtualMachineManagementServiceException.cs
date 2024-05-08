// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Exceptions;

public class VirtualMachineManagementServiceException : Exception
{
    public VirtualMachineManagementServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
