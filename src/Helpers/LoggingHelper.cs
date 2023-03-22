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
    public static void AccountStartupEvent(string eventName, string providerName, List<IDeveloperId> devIds)
    {
        var telemetryMessage = string.Empty;
        using var hasher = SHA256.Create();
        foreach (var devId in devIds)
        {
            var loginIdBytes = Encoding.ASCII.GetBytes(devId.LoginId());
            var hashedLoginId = hasher.ComputeHash(loginIdBytes);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hashedLoginId);
            }

            var hashedLoginIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);
            telemetryMessage += $"{hashedLoginIdString}_{providerName} , ";
        }

        // TODO: Instead of LoginId, hash a globally unique id of DeveloperId (like url)
        LoggerFactory.Get<ILogger>().Log($"{eventName}", LogLevel.Critical, $"{telemetryMessage}");
    }

    public static void AccountEvent(string eventName, string providerName, string loginId)
    {
        using var hasher = SHA256.Create();
        var loginIdBytes = Encoding.ASCII.GetBytes(loginId);
        var hashedLoginId = hasher.ComputeHash(loginIdBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(hashedLoginId);
        }

        var hashedLoginIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);

        // TODO: Instead of LoginId, hash a globally unique id of DeveloperId (like url)
        LoggerFactory.Get<ILogger>().Log($"{eventName}", LogLevel.Critical, $" developerId: {hashedLoginIdString}_{providerName}");
    }
}
