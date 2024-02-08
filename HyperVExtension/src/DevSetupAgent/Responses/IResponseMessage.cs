// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for providing response message data.
/// </summary>
public interface IResponseMessage
{
    string ResponseId { get; set; }

    string ResponseData { get; set; }
}
