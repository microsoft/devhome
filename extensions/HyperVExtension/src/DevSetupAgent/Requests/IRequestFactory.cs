// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for creating request handler based on request message.
/// </summary>
public interface IRequestFactory
{
    IHostRequest CreateRequest(IRequestContext requestContext);
}
