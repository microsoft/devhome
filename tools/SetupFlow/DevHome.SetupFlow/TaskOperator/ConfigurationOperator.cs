// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Contract.TaskOperator;
using Microsoft.Management.Configuration;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.TaskOperator;
public class ConfigurationOperator : IConfigurationOperator
{
    public IAsyncOperation<IApplyConfigurationResult> ApplyConfigurationAsync(StorageFile file)
    {
        return Task.Run<IApplyConfigurationResult>(async () =>
        {
            var taskResult = new ApplyConfigurationResult();

            try
            {
                var configurationFileHelper = new ConfigurationFileHelper(file);

                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Opening configuration set from file: {file.Path}");
                await configurationFileHelper.OpenConfigurationSetAsync();

                Log.Logger?.ReportInfo(Log.Component.Configuration, "Starting configuration set application");
                var result = await configurationFileHelper.ApplyConfigurationAsync();
                Log.Logger?.ReportInfo(Log.Component.Configuration, "Configuration application finished");

                taskResult.Attempted = true;
                taskResult.Succeeded = result.Succeeded;
                taskResult.RebootRequired = result.RequiresReboot;
                taskResult.UnitResults = result.Result.UnitResults.Select(unitResult => new ConfigurationUnitResult
                {
                    UnitName = unitResult.Unit.UnitName,
                    Intent = unitResult.Unit.Intent.ToString(),
                    IsSkipped = unitResult.State == ConfigurationUnitState.Skipped,
                    HResult = unitResult.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK,
                }).ToList<IConfigurationUnitResult>();

                if (result.ResultException != null)
                {
                    throw result.ResultException;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration.", e);
                taskResult.Succeeded = false;
            }

            return taskResult;
        }).AsAsyncOperation();
    }
}

public class ConfigurationUnitResult : IConfigurationUnitResult
{
    public int HResult { get; set; }

    public string Intent { get; set; }

    public bool IsSkipped { get; set; }

    public string UnitName { get; set; }
}

public class ApplyConfigurationResult : IApplyConfigurationResult
{
    public IList<IConfigurationUnitResult> UnitResults { get; set; }

    public bool Attempted { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}
