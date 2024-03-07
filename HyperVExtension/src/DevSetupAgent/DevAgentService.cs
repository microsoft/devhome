// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Telemetry;
using Microsoft.Windows.AppLifecycle;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Service to communicate between host and guest machines.
/// The main loop waits for messages from the host, processes them, and send response back to host.
/// </summary>
public class DevAgentService : BackgroundService
{
    private readonly IHost _host;

    public DevAgentService(IHost host)
    {
        _host = host;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logging.Logger()?.ReportInfo($"DevAgentService started at: {DateTimeOffset.Now}");

        try
        {
            var channel = _host.GetService<IHostChannel>();
            var requestManager = _host.GetService<IRequestManager>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await channel.WaitForMessageAsync(stoppingToken);

                    if (!stoppingToken.IsCancellationRequested && (message != null))
                    {
                        requestManager.ProcessRequestMessage(message, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logger()?.ReportError($"Exception in DevAgentService.", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Logger()?.ReportError($"Failed to run DevSetupAgent.", ex);
            throw;
        }
        finally
        {
            Logging.Logger()?.ReportInfo($"DevAgentService stopped at: {DateTimeOffset.Now}");
        }
    }
}
