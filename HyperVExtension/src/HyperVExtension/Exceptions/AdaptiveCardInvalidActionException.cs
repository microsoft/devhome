// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Exceptions;

public class AdaptiveCardInvalidActionException : Exception
{
    public AdaptiveCardInvalidActionException(string message)
        : base(message)
    {
    }
}
