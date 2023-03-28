// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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

    void HandleLogEvent(LogEvent evt);
}
