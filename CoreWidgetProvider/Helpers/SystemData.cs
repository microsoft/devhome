// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;

namespace CoreWidgetProvider.Helpers;

internal class SystemData : IDisposable
{
    public MemoryStats MemStats { get; set; }

    public SystemData()
    {
        MemStats = new MemoryStats();
    }

    public void Dispose()
    {
        MemStats.Dispose();
    }
}
