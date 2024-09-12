// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This is the stand-in service that is used when DevHome's NT service is not available. It's basically a dummy object that can
// be used so the rest of DevHome can still function without the service.
using DevHome.Service;

namespace DevHome.DevDiagnostics.Helpers;

internal sealed class StandInDevHomeService : IDevHomeService
{
    // Let folks subscribe to this event, but it will never be raised.
    public event MissingFileProcessLaunchFailureHandler? MissingFileProcessLaunchFailure
    {
        add { }
        remove { }
    }

    public StandInDevHomeService()
    {
    }
}
