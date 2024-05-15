// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Service to communicate between host and guest machines.
/// The main loop waits for messages from the host, processes them, and send response back to host.
/// </summary>
public class DevAgentService : BackgroundService
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(DevAgentService));
    private readonly IHost _host;

    public DevAgentService(IHost host)
    {
        _host = host;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Information($"DevAgentService started at: {DateTimeOffset.Now}");

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
                    _log.Error(ex, $"Exception in DevAgentService.");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to run DevSetupAgent.");
            throw;
        }
        finally
        {
            _log.Information($"DevAgentService stopped at: {DateTimeOffset.Now}");
        }
    }
}
