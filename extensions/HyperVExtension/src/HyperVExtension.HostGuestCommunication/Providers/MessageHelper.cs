// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;
using Serilog;

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
    public const string DevSetupPrefix = "DevSetup";
    public const string MessageIdStart = DevSetupPrefix + "{";
    private static readonly char[] CommunicationIdSeparators = { '{', '}' };

    public static bool IsValidMessageName(string[]? message, out int index, out int total)
    {
        // Number of parts separated by '-' DevSetup{<number>}-<index>-<total>
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

    private sealed class EqualityComparer : IEqualityComparer<(string name, int number)>
    {
        public bool Equals((string name, int number) x, (string name, int number) y)
        {
            return (x.number == y.number) && x.name.Equals(y.name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string name, int number) value)
        {
            return value.name.GetHashCode(StringComparison.OrdinalIgnoreCase) ^ value.number.GetHashCode();
        }
    }

    public static Dictionary<string, string> MergeMessageParts(Dictionary<string, string> messageParts)
    {
        var messages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var valueNames = messageParts.Keys.Where(k => k.StartsWith(MessageIdStart, StringComparison.OrdinalIgnoreCase)).ToList();
        HashSet<(string, int)> ignoreMessages = new(new EqualityComparer());

        foreach (var valueName in valueNames)
        {
            var s = valueName.Split(Separator);
            if (!IsValidMessageName(s, out var index, out var total))
            {
                continue;
            }

            // Use communication id (DevSetup{<number>}) and total number of message parts as a key to ignore messages
            // with the same id, but different total number of parts. This potentially could happen if we have stale messages
            // that were not cleaned up properly (say, the app crashed).
            if (ignoreMessages.Contains((s[0], total)))
            {
                continue;
            }

            // Count if we have all parts of the message
            var count = 0;
            foreach (var valueNameTmp in valueNames)
            {
                if (valueNameTmp.StartsWith(s[0] + $"{Separator}", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!IsValidMessageName(valueNameTmp.Split(Separator), out var indeTmp, out var totalTmp))
                    {
                        continue;
                    }

                    if (totalTmp == total)
                    {
                        count++;
                    }
                }
            }

            // Either we will process all parts of the message below
            // or will ignore it because we don't have all parts.
            // In both cases we don't want to iterate trough messages with the same id.
            ignoreMessages.Add((s[0], total));
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
                var log = Serilog.Log.ForContext("SourceContext", nameof(MessageHelper));
                log.Error(ex, $"Could not read guest message {valueName}");
            }

            messages.Add(name, value);
        }

        return messages;
    }

    public static Dictionary<string, string> GetRegistryMessageKvp(RegistryKey regKey)
    {
        var messageParts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

    /// <summary>
    /// Extract number from communication id ("DevSetup{<number>}").
    /// </summary>
    /// <param name="communicationId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Incorrect format of communication id</exception>
    public static uint GetCounterFromCommunicationId(string communicationId)
    {
        var parts = communicationId.Split(CommunicationIdSeparators);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid communication id");
        }

        return uint.Parse(parts[1], CultureInfo.InvariantCulture);
    }
}
