// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Services.DesiredStateConfiguration.Models;
using DevHome.SetupFlow.ElevatedComponent.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Windows.Foundation;

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
                taskResult.UnitResults = result.UnitResults.Select(unitResult => new ElevatedConfigureUnitTaskResult(unitResult)).ToList();

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
