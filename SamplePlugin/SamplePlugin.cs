// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Windows.Widgets.Providers;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SamplePlugin;

public sealed class SamplePlugin
{
    private readonly ManualResetEvent _pluginDisposedEvent;

    public SamplePlugin(ManualResetEvent pluginDisposedEvent)
    {
        this._pluginDisposedEvent = pluginDisposedEvent;
    }

    public void Dispose()
    {
        this._pluginDisposedEvent.Set();
    }
}
