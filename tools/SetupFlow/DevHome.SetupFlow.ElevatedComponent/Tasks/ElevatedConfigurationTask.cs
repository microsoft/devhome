// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Services.DesiredStateConfiguration.Models;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Configuration;
using Serilog;
using Windows.Foundation;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent.Tasks;

public sealed class ElevatedConfigurationTask
{
    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfiguration(string filePath, string content, Guid activityId)
    {
        var logger = LoggerFactory.Create(lb => lb.AddSerilog(dispose: false)).CreateLogger<ElevatedConfigurationTask>();
        return Task.Run(async () =>
        {
            var taskResult = new ElevatedConfigureTaskResult();

            try
            {
                var dsc = ElevatedComponentOperation.Host.Services.GetRequiredService<IDSC>();
                var file = DSCFile.CreateVirtual(filePath, content);
                var result = await dsc.ApplyConfigurationAsync(file, activityId);

                taskResult.TaskAttempted = true;
                taskResult.TaskSucceeded = result.Succeeded;
                taskResult.RebootRequired = result.RequiresReboot;
                taskResult.UnitResults = result.Result.UnitResults.Select(unitResult =>
                {
                    unitResult.Unit.Settings.TryGetValue("description", out var descriptionObj);
                    return new ElevatedConfigureUnitTaskResult
                    {
                        Type = unitResult.Unit.Type,
                        Id = unitResult.Unit.Identifier,
                        UnitDescription = descriptionObj?.ToString() ?? string.Empty,
                        Intent = unitResult.Unit.Intent.ToString(),
                        IsSkipped = unitResult.State == ConfigurationUnitState.Skipped,
                        HResult = unitResult.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK,
                        ResultSource = (int)(unitResult.ResultInformation?.ResultSource ?? ConfigurationUnitResultSource.None),
                        ErrorDescription = unitResult.ResultInformation?.Description,
                    };
                }).ToList();

                if (result.ResultException != null)
                {
                    throw result.ResultException;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to apply configuration.");
                taskResult.TaskSucceeded = false;
            }

            return taskResult;
        }).AsAsyncOperation();
    }
}
