// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.DistributionDefinitions;

namespace WSLExtension.Models;

public class WslInstallDistributionOperation : ICreateComputeSystemOperation
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslInstallDistributionOperation));

    private readonly string _preparingToInstall;

    private readonly string _waitingToComplete;

    private readonly string _installationFailedTimeout;

    private readonly string _installationSuccessful;

    private readonly TimeSpan _threeSecondDelayInSeconds = TimeSpan.FromSeconds(3);

    private readonly DistributionDefinition _definition;

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public WslInstallDistributionOperation(
        DistributionDefinition distributionDefinition,
        IStringResource stringResource,
        IWslManager wslManager)
    {
        _definition = distributionDefinition;
        _stringResource = stringResource;
        _wslManager = wslManager;
        _preparingToInstall = GetLocalizedString("WSLPrepareInstall", _definition.FriendlyName);
        _waitingToComplete = GetLocalizedString("WSLWaitingToCompleteInstallation", _definition.FriendlyName);

        _installationFailedTimeout = GetLocalizedString("WSLInstallationFailedTimeOut", _definition.FriendlyName);

        _installationSuccessful = GetLocalizedString("WSLInstallationCompletedSuccessfully", _definition.FriendlyName);
    }

    private string GetLocalizedString(string resourcekey, string value)
    {
        return _stringResource.GetLocalized(resourcekey, value);
    }

    public IAsyncOperation<CreateComputeSystemResult> StartAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                var startTime = DateTime.UtcNow;
                _log.Information($"Starting installation for {_definition.Name}");
                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_preparingToInstall, 0));

                // Cancel waiting for install if the distribution hasn't been installed after 10 minutes.
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(10));
                WslRegisteredDistribution? registeredDistribution = null;
                var distributionInstalledSuccessfully = false;
                await _wslManager.InstallDistributionAsync(_definition.Name);

                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_waitingToComplete, 0));
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Wait in 3 second intervals before checking. Unfortunately there are no APIs to check for
                    // installation so we need to keep checking for its completion.
                    await Task.Delay(_threeSecondDelayInSeconds);
                    registeredDistribution = await _wslManager.GetInformationOnRegisteredDistributionAsync(_definition.Name);

                    if ((registeredDistribution != null) &&
                        (distributionInstalledSuccessfully = registeredDistribution.IsDistributionFullyRegistered()))
                    {
                        break;
                    }
                }

                _log.Information($"Ending installation for {_definition.Name}. Operation took: {DateTime.UtcNow - startTime}");
                if (distributionInstalledSuccessfully)
                {
                    Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_installationSuccessful, 100));
                    return new CreateComputeSystemResult(new WslComputeSystem(_stringResource, registeredDistribution!, _wslManager));
                }

                throw new TimeoutException(_installationFailedTimeout);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to install {_definition.FriendlyName} due to exception");
                var errorMsg = _stringResource.GetLocalized("WSLInstallationFailedWithException", _definition.FriendlyName, ex.Message);
                return new CreateComputeSystemResult(ex, errorMsg, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired
    {
        add { }
        remove { }
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;
}
