// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Service.Runtime;

public class DevHomeService : IDevHomeService
{
    public DevHomeService()
    {
    }

    public int GetNumber()
    {
        return 42;
    }
}
