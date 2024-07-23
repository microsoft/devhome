// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace HyperVExtension.DevSetupAgent;

internal sealed class NtStatusException : Exception
{
    public NtStatusException()
    {
    }

    public NtStatusException(string? message)
        : base(message)
    {
    }

    public NtStatusException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public NtStatusException(string? message, int ntStatus)
        : base(message)
    {
        // NTStatus is not an HRESULT, but we will uonly use it to pass error back to the caller
        // for diagnostic. Conversion to HRESULT can be done in more that one way and can be not 1 to 1 mapping anyway
        HResult = ntStatus;
    }
}
