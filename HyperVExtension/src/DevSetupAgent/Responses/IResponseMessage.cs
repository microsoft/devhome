// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for providing response message data.
/// </summary>
public interface IResponseMessage
{
    string ResponseId { get; set; }

    string ResponseData { get; set; }
}
