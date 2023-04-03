// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Logging.Listeners;

public abstract class ListenerBase : IListener
{
    public ILoggerHost? Host
    {
        get;
        set;
    }

    public Options? Options => Host?.Options;

    public string Name
    {
        get;
    }

    public ListenerBase(string name)
    {
        Host = null;
        Name = name;
    }

    public abstract void HandleLogEvent(LogEvent evt);
}
