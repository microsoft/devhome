// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging.Listeners;

public interface IListener
{
    string Name
    {
        get;
    }

    ILoggerHost? Host
    {
        get;
        set;
    }

    void HandleLogEvent(LogEvent logEvent);
}
