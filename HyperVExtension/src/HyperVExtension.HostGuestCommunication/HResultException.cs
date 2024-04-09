// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.HostGuestCommunication;

public sealed class HResultException : Exception
{
    public HResultException(int resultCode, string? description = null)
        : base(description)
    {
        HResult = resultCode;
    }
}
