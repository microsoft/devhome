// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;
using Windows.Win32.Foundation;

namespace HyperVExtension.Exceptions;

internal sealed class GuestOsOperationNotSupportedException : GuestOsVersionException
{
    public GuestOsOperationNotSupportedException(IStringResource stringResource, Dictionary<string, string>? guestOsProperties)
        : base(stringResource.GetLocalized("GuestOsOperationNotSupported"), guestOsProperties)
    {
        HResult = HRESULT.E_NOTSUPPORTED;
    }
}
