// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.SetupFlow.Exceptions;

public class SDKOpenConfigurationSetResultException : Exception
{
    public SDKOpenConfigurationSetResultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SDKOpenConfigurationSetResultException(string message)
        : base(message)
    {
    }
}
