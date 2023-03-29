// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Models;
using DevHome.Telemetry;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.ConfigurationFile.Models;

internal class ConfigureTask : ISetupTask
{
    private readonly StorageFile _file;
    private readonly ILogger _logger;

    public bool RequiresAdmin => false;

    public bool RequiresReboot => false;

    public ConfigureTask(ILogger logger, StorageFile file)
    {
        _logger = logger;
        _file = file;
    }

    public LoadingMessages GetLoadingMessages()
    {
        return new ()
        {
            Executing = "Applying configuration",
            Error = "Failed to apply configuration",
            Finished = "Applied configuration successfully",
        };
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            try
            {
                var factory = new ConfigurationSetProcessorFactory(ConfigurationProcessorType.Hosted, null);
                var processor = new ConfigurationProcessor(factory);
                var openResult = processor.OpenConfigurationSet(await _file.OpenReadAsync());
                if (openResult.Set is null)
                {
                    throw new InvalidOperationException($"{openResult.ResultCode}, {openResult.Field}");
                }

                var set = openResult.Set;
                var apply = processor.ApplySetAsync(openResult.Set, ApplyConfigurationSetFlags.None);
                apply.Progress += (a, b) =>
                {
                    Debug.WriteLine(a);
                    Debug.WriteLine(b);
                };
                var result = await apply;
                if (result.ResultCode != null)
                {
                    throw result.ResultCode;
                }

                return TaskFinishedState.Success;
            }
            catch
            {
                _logger.LogError(nameof(ConfigureTask), LogLevel.Local, "Failed to apply configuration");
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }
}
