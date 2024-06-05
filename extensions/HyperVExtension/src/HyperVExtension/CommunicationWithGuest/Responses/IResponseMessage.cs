// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Interface for providing response message data.
/// </summary>
public interface IResponseMessage
{
    string CommunicationId { get; set; }

    string ResponseData { get; set; }
}
