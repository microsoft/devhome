// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace DevHome.Service.Runtime;

[ComVisible(true)]
public class DevHomeService : IDevHomeService, IDisposable
{
    public DevHomeService()
    {
        ComHelpers.VerifyCaller();
    }

    ~DevHomeService()
    {
        Dispose();
    }

    public int GetNumber()
    {
        ComHelpers.VerifyCaller();
        return 42;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
