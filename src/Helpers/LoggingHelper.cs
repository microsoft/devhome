// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using DevHome.Telemetry;

namespace DevHome.Helpers;
public static class LoggingHelper
{
    public static void AccountEvent_Critical(string eventName, string providerName, string loginId)
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
        LoggerFactory.Get<ILogger>().Log($"{eventName}", LogLevel.Critical, $"DevIdProvider: {providerName} developerId: {hashedLoginIdString}");
    }
}
