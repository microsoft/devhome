// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;
using System.Text;
using HyperVExtension.Common;
using HyperVExtension.Helpers;

namespace HyperVExtension.Exceptions;

internal class GuestOsVersionException : Exception
{
    public GuestOsVersionException(IStringResource stringResource, Exception innerException, Dictionary<string, string>? guestOsProperties)
        : base(stringResource.GetLocalized("FailedToDetermineGuestOsVersion", $"Windows {Constants.MinWindowsVersionForApplyConfiguration}"), innerException)
    {
        HResult = innerException.HResult;
        GuestOsProperties = guestOsProperties;
    }

    protected GuestOsVersionException(string message, Dictionary<string, string>? guestOsProperties)
        : base(message)
    {
        GuestOsProperties = guestOsProperties;
    }

    public Dictionary<string, string>? GuestOsProperties { get; }

    public override string ToString()
    {
        StringBuilder message = new();
        message.Append(CultureInfo.InvariantCulture, $"{Message}.");
        if (InnerException != null)
        {
            message.Append(CultureInfo.InvariantCulture, $" {InnerException}.");
        }

        if (GuestOsProperties != null)
        {
            GuestOsProperties.TryGetValue(HyperVStrings.OSPlatformId, out var osPlatformId);
            GuestOsProperties.TryGetValue(HyperVStrings.OSVersion, out var osVersion);
            GuestOsProperties.TryGetValue(HyperVStrings.OSName, out var osName);
            if ((osPlatformId != null) ||
                (osVersion != null) ||
                (osName != null))
            {
                message.Append(" (");
                if (osPlatformId != null)
                {
                    message.Append(CultureInfo.InvariantCulture, $"{HyperVStrings.OSPlatformId} = {osPlatformId}");
                }

                if (osVersion != null)
                {
                    message.Append(CultureInfo.InvariantCulture, $", {HyperVStrings.OSVersion} = {osVersion}");
                }

                if (osName != null)
                {
                    message.Append(CultureInfo.InvariantCulture, $", {HyperVStrings.OSName} = {osName}");
                }

                message.Append(')');
            }
        }

        return message.ToString();
    }
}
