// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Configuration;
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

                Log.Logger?.ReportInfo(nameof(ElevatedConfigurationTask), $"Opening configuration set from file: {file.Path}");
                await configurationFileHelper.OpenConfigurationSetAsync();

                Log.Logger?.ReportInfo(nameof(ElevatedConfigurationTask), "Starting configuration set application");
                await configurationFileHelper.ApplyConfigurationAsync();
                Log.Logger?.ReportInfo(nameof(ElevatedConfigurationTask), "Configuration application finished");

                taskResult.TaskAttempted = true;
                taskResult.TaskSucceeded = configurationFileHelper.ApplicationSucceeded;
                taskResult.RebootRequired = configurationFileHelper.ResultRequiresReboot;

                if (configurationFileHelper.ResultException != null)
                {
                    throw configurationFileHelper.ResultException;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(nameof(ElevatedConfigurationTask), $"Failed to apply configuration: {e.Message}");
                taskResult.TaskSucceeded = false;
            }

            return taskResult;
        }).AsAsyncOperation();
    }
}
