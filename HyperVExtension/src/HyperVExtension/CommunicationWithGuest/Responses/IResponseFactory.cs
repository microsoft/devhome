// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Interface for creating response handler based on response message.
/// </summary>
public interface IResponseFactory
{
    IGuestResponse CreateResponse(IResponseMessage message);
}
