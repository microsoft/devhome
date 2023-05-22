// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Microsoft.Management.Configuration;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent.Tasks;

public sealed class ElevatedConfigurationTask
{
    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfiguration(StorageFile file)
    {
        return Task.Run(async () =>
        {
            var taskResult = new ElevatedConfigureTaskResult();

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
                taskResult.UnitResults = result.Result.UnitResults.Select(unitResult => new ElevatedConfigureUnitTaskResult
                {
                    UnitName = unitResult.Unit.UnitName,
                    Intent = unitResult.Unit.Intent.ToString(),
                    IsSkipped = unitResult.State == ConfigurationUnitState.Skipped,
                    HResult = unitResult.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK,
                }).ToList();

                if (result.ResultException != null)
                {
                    throw result.ResultException;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration.", e);
                taskResult.TaskSucceeded = false;
            }

            return taskResult;
        }).AsAsyncOperation();
    }
}
