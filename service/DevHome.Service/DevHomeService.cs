// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DevHome.Service.Runtime;

[ComVisible(true)]
public class DevHomeService : IDevHomeService
{
    public DevHomeService()
    {
        Process myCaller = ComHelpers.GetClientProcess();

        ComHelpers.VerifyCaller(myCaller);

        // Track our caller process
        ServiceLifetimeController.RegisterProcess(myCaller);
    }

    public int GetNumber()
    {
        return 42;
    }
}
