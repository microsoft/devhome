// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for creating request handler based on request message.
/// </summary>
public interface IRequestFactory
{
    IHostRequest CreateRequest(IRequestMessage message);
}
