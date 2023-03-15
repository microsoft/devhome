// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Factory for objects that run in the elevated background process.
/// </summary>
public sealed class ElevatedComponentFactory : IElevatedComponentFactory
{
    public void TestMethod()
    {
        Console.WriteLine("TestMethod!");
    }
}
