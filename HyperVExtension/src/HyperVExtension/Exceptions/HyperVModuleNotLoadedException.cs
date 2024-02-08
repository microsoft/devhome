// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Exceptions;

public class HyperVModuleNotLoadedException : HyperVManagerException
{
    public HyperVModuleNotLoadedException(string message)
        : base(message)
    {
    }
}
