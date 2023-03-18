// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Helpers;
public static class LoggingHelper
{
    public static void LoginEvent_Critical(string providerName, string loginId)
    {
        LoggerFactory.Get<ILogger>().Log($"LoggedInEvent", LogLevel.Critical, $"DevIdProvider: {providerName} developerId: {loginId}");
    }

    public static void LogoutEvent_Critical(string providerName, string loginId)
    {
        LoggerFactory.Get<ILogger>().Log($"LoggedOutEvent", LogLevel.Critical, $"DevIdProvider: {providerName} developerId: {loginId}");
    }
}
