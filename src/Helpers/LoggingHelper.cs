// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Helpers;
public static class LoggingHelper
{
    public static void Critical_AccountStartupEvent(string eventName, string providerName, List<IDeveloperId> devIds)
    {
        string telemetryMessage;
        using SHA256 mySHA256 = SHA256.Create();
        foreach (var devId in devIds)
        {
            var loginIdBytes = Encoding.ASCII.GetBytes(devId.LoginId());
            var hashedLoginId = mySHA256.ComputeHash(loginIdBytes);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hashedLoginId);
            }

            var hashedLoginIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);
            Trace.WriteLine("Hash value: " + hashedLoginIdString);

            telemetryMessage += $"{hashedLoginIdString}_{providerName}" + Environment.NewLine;
        }

        // TODO: Instead of LoginId, hash a globally unique id of DeveloperId (like url)
        LoggerFactory.Get<ILogger>().Log($"{eventName}", LogLevel.Critical, $"{telemetryMessage}");
    }

    public static void Critical_AccountEvent(string eventName, string providerName, string loginId)
    {
        using SHA256 mySHA256 = SHA256.Create();
        var loginIdBytes = Encoding.ASCII.GetBytes(loginId);
        var hashedLoginId = mySHA256.ComputeHash(loginIdBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(hashedLoginId);
        }

        var hashedLoginIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);
        Trace.WriteLine("Hash value: " + hashedLoginIdString);

        // TODO: Instead of LoginId, hash a globally unique id of DeveloperId (like url)
        LoggerFactory.Get<ILogger>().Log($"{eventName}", LogLevel.Critical, $" developerId: {hashedLoginIdString}_{providerName}");
    }
}
