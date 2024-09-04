// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace DevHome.Service.Runtime;

[ComVisible(true)]
public class DevHomeService : IDevHomeService
{
    public DevHomeService()
    {
        ComHelpers.VerifyCaller();

        // Track our caller process
        ServiceLifetimeController.RegisterProcess(ComHelpers.GetClientProcess());
    }

    public int GetNumber()
    {
        return 42;
    }
}
