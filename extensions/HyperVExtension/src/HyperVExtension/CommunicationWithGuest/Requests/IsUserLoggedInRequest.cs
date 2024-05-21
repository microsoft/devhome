// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Ask Hyper-V VM if user is logged in.
/// </summary>
internal sealed class IsUserLoggedInRequest : RequestBase
{
    public const string RequestTypeId = "IsUserLoggedIn";

    public IsUserLoggedInRequest()
        : base(RequestTypeId)
    {
        GenerateJsonData();
    }
}
