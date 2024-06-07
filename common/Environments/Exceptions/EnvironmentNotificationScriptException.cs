// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Common.Environments.Exceptions;

public class EnvironmentNotificationScriptException : Exception
{
    public EnvironmentNotificationScriptException(string message)
        : base(message)
    {
    }
}
