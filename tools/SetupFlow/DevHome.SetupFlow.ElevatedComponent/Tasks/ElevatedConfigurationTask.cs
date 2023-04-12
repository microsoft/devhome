// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.ElevatedComponent.Tasks;

public sealed class ElevatedConfigurationTask
{
    public IAsyncOperation<TaskResult> ApplyConfiguration(StorageFile file)
    {
        return Task.Run(async () =>
        {
            var taskResult = new TaskResult();

            try
            {
                var configurationFileHelper = new ConfigurationFileHelper(file);

                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Opening configuration set from file: {file.Path}");
                await configurationFileHelper.OpenConfigurationSetAsync();

                Log.Logger?.ReportInfo(Log.Component.Configuration, "Starting configuration set application");
                var result = await configurationFileHelper.ApplyConfigurationAsync();
                Log.Logger?.ReportInfo(Log.Component.Configuration, "Configuration application finished");

                taskResult.TaskAttempted = true;
                taskResult.TaskSucceeded = result.Succeeded;
                taskResult.RebootRequired = result.RequiresReboot;

                if (result.ResultException != null)
                {
                    throw result.ResultException;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration: {e.Message}");
                taskResult.TaskSucceeded = false;
            }

            return taskResult;
        }).AsAsyncOperation();
    }
}
