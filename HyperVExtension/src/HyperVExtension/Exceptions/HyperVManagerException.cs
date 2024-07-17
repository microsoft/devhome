// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Exceptions;

public class HyperVManagerException : Exception
{
    public HyperVManagerException(string? message)
        : base(message)
    {
    }

    public HyperVManagerException(string? message, Exception? innerException)
    : base(message, innerException)
    {
    }
}
