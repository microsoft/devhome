// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.Exceptions;

public class SDKOpenConfigurationSetResultException : Exception
{
    /// <summary>
    /// Gets the <see cref="OpenConfigurationSetResult.ResultCode"/>
    /// </summary>
    public Exception ResultCode
    {
        get;
    }

    /// <summary>
    /// Gets the field that is missing/invalid, if appropriate for the specific ResultCode.
    /// </summary>
    public string Field
    {
        get;
    }

    /// <summary>
    /// Gets the value of the field, if appropriate for the specific ResultCode.
    /// </summary>
    public string Value
    {
        get;
    }

    internal SDKOpenConfigurationSetResultException(Exception resultCode, string field, string value)
    {
        ResultCode = resultCode;
        Field = field;
        Value = value;
    }
}
