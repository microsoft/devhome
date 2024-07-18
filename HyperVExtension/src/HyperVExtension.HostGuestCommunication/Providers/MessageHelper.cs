// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace HyperVExtension.HostGuestCommunication;

/// <summary>
/// A class that contains extension methods for <see cref="string"/>.
/// </summary>
/// <remarks>
/// Returns enumerator to get substrings of a give length.
/// </remarks>
public static class MessageHelper
{
    public const char Separator = '~';
    public const string MessageIdStart = "DevSetup{";

    public static bool IsValidMessageName(string[]? message, out int index, out int total)
    {
        // Number of parts separated by '-' DevSetup{<GUID>}-<index>-<total>
        const int ValueNamePartsCount = 3;
        index = 0;
        total = 0;
        if (message == null)
        {
            return false;
        }

        if (message.Length != ValueNamePartsCount)
        {
            return false;
        }

        if (!int.TryParse(message[1], out index) || !int.TryParse(message[2], out total))
        {
            return false;
        }

        return true;
    }

    public static Dictionary<string, string> MergeMessageParts(Dictionary<string, string> messageParts)
    {
        var messages = new Dictionary<string, string>();
        var guestMessages = new Dictionary<string, string>();
        var valueNames = messageParts.Keys.Where(k => k.StartsWith(MessageHelper.MessageIdStart, StringComparison.OrdinalIgnoreCase)).ToList();
        HashSet<string> ignoreMessages = new();

        foreach (var valueName in valueNames)
        {
            var s = valueName.Split(Separator);
            if (!MessageHelper.IsValidMessageName(s, out var index, out var total))
            {
                continue;
            }

            if (ignoreMessages.Contains(s[0]))
            {
                continue;
            }

            // Count if we have all parts of the message
            var count = 0;
            foreach (var valueNameTmp in valueNames)
            {
                if (valueNameTmp.StartsWith(s[0] + $"{Separator}", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!MessageHelper.IsValidMessageName(valueNameTmp.Split(Separator), out var indeTmp, out var totalTmp))
                    {
                        continue;
                    }

                    count++;
                }
            }

            // Either we will process all parts of the message below
            // or will ignore it because we don't have all parts.
            // In both cases we don't want to iterate trough messages with the same id.
            ignoreMessages.Add(s[0]);
            if (count != total)
            {
                // Ignore this message for now. We don't have all parts.
                continue;
            }

            // Merge all parts of the message
            // Preserve message GUID, delete the value and create response even if reading failed.
            var name = s[0];
            var value = string.Empty;
            try
            {
                var sb = new StringBuilder();
                for (var i = 1; i <= total; i++)
                {
                    var value1 = messageParts[s[0] + $"{Separator}{i}{Separator}{total}"];
                    if (value1 == null)
                    {
                        throw new InvalidOperationException($"Could not read guest message {valueName}");
                    }

                    sb.Append(value1);
                }

                value = sb.ToString();
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError($"Could not read guest message {valueName}", ex);
            }

            messages.Add(name, value);
        }

        return messages;
    }

    public static Dictionary<string, string> GetRegistryMessageKvp(RegistryKey regKey)
    {
        var messageParts = new Dictionary<string, string>();
        foreach (var valueName in regKey.GetValueNames())
        {
            if (!valueName.StartsWith(MessageIdStart, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = regKey.GetValue(valueName);
            if (value is string str)
            {
                messageParts.Add(valueName, str);
            }
        }

        return messageParts;
    }

    /// <summary>
    /// Search and delete all existing registry values with names starting with startsWith
    /// </summary>
    /// <param name="registryKey">Parent registry key.</param>
    /// <param name="registryKeyPath">Registry key sub-path to search.</param>
    /// <param name="startsWith">Beginning of the value name.</param>
    public static void DeleteAllMessages(RegistryKey registryKey, string registryKeyPath, string startsWith)
    {
        var regKey = registryKey.OpenSubKey(registryKeyPath, true);
        var values = regKey?.GetValueNames();
        if (values != null)
        {
            foreach (var value in values)
            {
                if (value.StartsWith(startsWith, StringComparison.InvariantCultureIgnoreCase))
                {
                    regKey!.DeleteValue(value, false);
                }
            }
        }
    }
}
