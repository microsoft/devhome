// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WSLExtension.Helpers;

/// <summary>
///     Helper methods for working with <see cref="Guid" />.
/// </summary>
public static class GuidUtility
{
    /// <summary>
    ///     Tries to parse the specified string as a <see cref="Guid" />.  A return value indicates whether the operation
    ///     succeeded.
    /// </summary>
    /// <param name="value">The GUID string to attempt to parse.</param>
    /// <param name="theGuid">
    ///     When this method returns, contains the <see cref="Guid" /> equivalent to the GUID
    ///     contained in <paramref name="value" />, if the conversion succeeded, or Guid.Empty if the conversion failed.
    /// </param>
    /// <returns><c>true</c> if a GUID was successfully parsed; <c>false</c> otherwise.</returns>
    public static bool TryParse(string value, out Guid theGuid)
    {
        return Guid.TryParse(value, out theGuid);
    }

    /// <summary>
    ///     Converts a GUID to a lowercase string with no dashes.
    /// </summary>
    /// <param name="theGuid">The GUID.</param>
    /// <returns>The GUID as a lowercase string with no dashes.</returns>
    public static string ToLowerNoDashString(this Guid theGuid)
    {
        return theGuid.ToString("N");
    }

    /// <summary>
    ///     Attempts to convert a lowercase, no dashes string to a GUID.
    /// </summary>
    /// <param name="value">The string.</param>
    /// <returns>The GUID, if the string could be converted; otherwise, null.</returns>
    public static Guid? TryFromLowerNoDashString(string value)
    {
        return !TryParse(value, out var guid) || value != guid.ToLowerNoDashString() ? default(Guid?) : guid;
    }

    /// <summary>
    ///     Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
    /// </summary>
    /// <param name="namespaceId">The ID of the namespace.</param>
    /// <param name="name">The name (within that namespace).</param>
    /// <returns>A UUID derived from the namespace and name.</returns>
    public static Guid Create(Guid namespaceId, string name)
    {
        return Create(namespaceId, name, 5);
    }

    /// <summary>
    ///     Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
    /// </summary>
    /// <param name="namespaceId">The ID of the namespace.</param>
    /// <param name="name">The name (within that namespace).</param>
    /// <param name="version">
    ///     The version number of the UUID to create; this value must be either
    ///     3 (for MD5 hashing) or 5 (for SHA-1 hashing).
    /// </param>
    /// <returns>A UUID derived from the namespace and name.</returns>
    public static Guid Create(Guid namespaceId, string name, int version)
    {
        ArgumentNullException.ThrowIfNull(name);

        // convert the name to a sequence of octets (as defined by the standard or conventions of its namespace) (step 3)
        // ASSUME: UTF-8 encoding is always appropriate
        return Create(namespaceId, Encoding.UTF8.GetBytes(name), version);
    }

    /// <summary>
    ///     Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
    /// </summary>
    /// <param name="namespaceId">The ID of the namespace.</param>
    /// <param name="nameBytes">The name (within that namespace).</param>
    /// <returns>A UUID derived from the namespace and name.</returns>
    public static Guid Create(Guid namespaceId, byte[] nameBytes)
    {
        return Create(namespaceId, nameBytes, 5);
    }

    /// <summary>
    ///     Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
    /// </summary>
    /// <param name="namespaceId">The ID of the namespace.</param>
    /// <param name="nameBytes">The name (within that namespace).</param>
    /// <param name="version">
    ///     The version number of the UUID to create; this value must be either
    ///     3 (for MD5 hashing) or 5 (for SHA-1 hashing).
    /// </param>
    /// <returns>A UUID derived from the namespace and name.</returns>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Per spec.")]
    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Per spec.")]
    public static Guid Create(Guid namespaceId, byte[] nameBytes, int version)
    {
        if (version != 3 && version != 5)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "version must be either 3 or 5.");
        }

        // convert the namespace UUID to network order (step 3)
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        // compute the hash of the namespace ID concatenated with the name (step 4)
        var data = namespaceBytes.Concat(nameBytes).ToArray();
        byte[] hash;
        using (var algorithm = version == 3 ? (HashAlgorithm)MD5.Create() : SHA1.Create())
        {
            hash = algorithm.ComputeHash(data);
        }

        // most bytes from the hash are copied straight to the bytes of the new GUID (steps 5-7, 9, 11-12)
        var newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        // set the four most significant bits (bits 12 through 15) of the time_hi_and_version field to the appropriate 4-bit version number from Section 4.1.3 (step 8)
        newGuid[6] = (byte)((newGuid[6] & 0x0F) | (version << 4));

        // set the two most significant bits (bits 6 and 7) of the clock_seq_hi_and_reserved to zero and one, respectively (step 10)
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

        // convert the resulting UUID to local byte order (step 13)
        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    // Converts a GUID (expressed as a byte array) to/from network order (MSB-first).
    internal static void SwapByteOrder(byte[] guid)
    {
        SwapBytes(guid, 0, 3);
        SwapBytes(guid, 1, 2);
        SwapBytes(guid, 4, 5);
        SwapBytes(guid, 6, 7);
    }

    private static void SwapBytes(byte[] guid, int left, int right)
    {
        var temp = guid[left];
        guid[left] = guid[right];
        guid[right] = temp;
    }
}
