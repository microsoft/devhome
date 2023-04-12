// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Helpers;
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
        Log.Logger?.ReportInfo(Log.Component.ElevatedComponent, "Creating elevated package installer");
        return new ElevatedInstallTask();
    }

    public DevDriveStorageOperator CreateDevDriveStorageOperator()
    {
        Log.Logger?.ReportInfo(Log.Component.ElevatedComponent, "Creating elevated Dev Drive storage operator");
        return new DevDriveStorageOperator();
    }

    public ElevatedConfigurationTask CreateElevatedConfigurationTask()
    {
        Log.Logger?.ReportInfo(Log.Component.ElevatedComponent, "Creating elevated Configuration File applier");
        return new ElevatedConfigurationTask();
    }
}
