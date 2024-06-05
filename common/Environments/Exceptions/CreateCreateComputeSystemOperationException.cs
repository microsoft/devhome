// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Environments.Exceptions;

public class CreateCreateComputeSystemOperationException : Exception
{
    public CreateCreateComputeSystemOperationException(string message)
        : base(message)
    {
    }
}
