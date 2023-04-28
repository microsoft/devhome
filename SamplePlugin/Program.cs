// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel.Background;
using Windows.Globalization.DateTimeFormatting;

namespace SamplePlugin;

public class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            using var server = new RegistrationManager();
            var pluginDisposedEvent = new ManualResetEvent(false);

            var devIdProvider = new DevIDProvider();
            var repoProvider = new RepositoryProvider();

            // We are instantiating provider instances once above, and returning it every time the callback in RegisterProvider below is called.
            // This makes sure that only one instance of provider object is alive, which is returned every time the host asks for the IProvider object.
            // If you want to instantiate a new instance each time the host asks, create the new instance inside the lambda function.
            server.RegisterProvider(() => devIdProvider);
            server.RegisterProvider(() => repoProvider);

            // This will make the main thread wait until the event is signalled by the plugin class.
            // Since we have single instance of the plugin object, we exit as sooon as it is disposed.
            pluginDisposedEvent.WaitOne();
        }
        else
        {
            Console.WriteLine("Not being launched as a Plugin... exiting.");
        }
    }
}
