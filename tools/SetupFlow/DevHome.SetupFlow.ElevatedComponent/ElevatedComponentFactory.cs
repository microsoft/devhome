// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ElevatedComponent.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Tasks;

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

    public ElevatedInstallTask CreateElevatedInstallTask()
    {
        Log.Logger?.ReportInfo(nameof(ElevatedComponentFactory), "Creating elevated package installer");
        return new ElevatedInstallTask();
    }

    public DevDriveStorageOperator CreateDevDriveStorageOperator()
    {
        Log.Logger?.ReportInfo(nameof(ElevatedComponentFactory), "Creating elevated Dev Drive storage operator");
        return new DevDriveStorageOperator();
    }

    public ElevatedConfigurationTask CreateElevatedConfigurationTask()
    {
        Log.Logger?.ReportInfo(nameof(ElevatedComponentFactory), "Creating elevated Configuration File applier");
        return new ElevatedConfigurationTask();
    }
}
