// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Base class for error responses.
/// </summary>
internal abstract class ErrorResponseBase : ResponseBase
{
    public ErrorResponseBase(string requestId)
        : base(requestId)
    {
    }

    public abstract string Error
    {
        get;
    }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();
        JsonData![nameof(Error)] = Error;
    }
}
