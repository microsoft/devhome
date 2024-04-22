// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

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
