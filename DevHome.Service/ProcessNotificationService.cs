// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using DevHome.Service;
using Windows.Foundation;
using WinRT;

namespace DevHome.Service.Runtime;

[ComVisible(true)]
[Guid("1F98F450-C163-4A99-B257-E1E6CB3E1C57")]
[ComDefaultInterface(typeof(ITimServer))]
public class ProcessNotificationService : ITimServer
{
    public ProcessNotificationService()
    {
    }

    public int GetNumber()
    {
        return 24;
    }
}
