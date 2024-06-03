// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.HostGuestCommunication;

/// <summary>
/// A class that contains extension methods for <see cref="string"/>.
/// </summary>
/// <remarks>
/// Returns enumerator to get substrings of a give length.
/// </remarks>
public static class StringExtensions
{
    public static IEnumerable<string> SplitByLength(this string str, int maxLength)
    {
        for (var startIndex = 0; startIndex < str.Length; startIndex += maxLength)
        {
            yield return str.Substring(startIndex, Math.Min(maxLength, str.Length - startIndex));
        }
    }
}
