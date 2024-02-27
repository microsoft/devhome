// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.SetupFlow.Exceptions;

public class SDKApplyConfigurationSetResultException : Exception
{
    public SDKApplyConfigurationSetResultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SDKApplyConfigurationSetResultException(string message)
    : base(message)
    {
    }
}
