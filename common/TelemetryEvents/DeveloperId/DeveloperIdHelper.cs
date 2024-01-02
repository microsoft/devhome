// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents.DeveloperId;
public static class DeveloperIdHelper
{
    public static string GetHashedDeveloperId(string providerName, IDeveloperId devId)
    {
        // TODO: Instead of LoginId, hash a globally unique id of DeveloperId (like url)
        // https://github.com/microsoft/devhome/issues/611
        using var hasher = SHA256.Create();
        var loginIdBytes = Encoding.ASCII.GetBytes(devId.LoginId);
        var hashedLoginId = hasher.ComputeHash(loginIdBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(hashedLoginId);
        }

        var hashedLoginIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);
        var hashedWindowsIdString = "UNKNOWN";

        // Include Hashed WindowsId if available
        var loginSessionId = WindowsIdentity.GetCurrent().Name;

        if (loginSessionId != null)
        {
            loginIdBytes = Encoding.ASCII.GetBytes(loginSessionId);

            hashedLoginId = hasher.ComputeHash(loginIdBytes);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hashedLoginId);
            }

            hashedWindowsIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);
        }

        return $"{hashedLoginIdString}_{hashedWindowsIdString}_{providerName}";
    }
}
