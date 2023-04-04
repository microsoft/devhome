// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ElevatedComponent.AppManagement;
using DevHome.SetupFlow.ElevatedComponent.Helpers;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Factory for objects that run in the elevated background process.
/// </summary>
public sealed class ElevatedComponentFactory : IElevatedComponentFactory
{
    public void WriteToStdOut(string value)
    {
        Console.WriteLine(value);
    }

    public PackageInstaller CreatePackageInstaller()
    {
        Log.Logger?.ReportInfo(nameof(ElevatedComponentFactory), "Creating elevated package installer");
        return new PackageInstaller();
    }
}
