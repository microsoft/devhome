// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Contracts;

namespace WSLExtension.Models;

public class WslInstallAndRegisterDistributionOperation : ICreateComputeSystemOperation
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslInstallAndRegisterDistributionOperation));

    private readonly string _preparingToInstall;

    private readonly string _waitingToComplete;

    private readonly string _installationFailedTimeout;

    private readonly string _installationSuccessful;

    private readonly TimeSpan _tenMinuteTimeout = TimeSpan.FromMinutes(10);

    private readonly TimeSpan _threeSecondDelayInSeconds = TimeSpan.FromSeconds(3);

    private readonly WslDistributionInfo _distributionInfo;

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public WslInstallAndRegisterDistributionOperation(
        WslDistributionInfo distributionInfo,
        IStringResource stringResource,
        IWslManager wslManager)
    {
        _distributionInfo = distributionInfo;
        _stringResource = stringResource;
        _wslManager = wslManager;
        _preparingToInstall = _stringResource.GetLocalized("WSLPrepareInstall", _distributionInfo.FriendlyName);
        _waitingToComplete = _stringResource.GetLocalized("WSLWaitingToCompleteInstallation", _distributionInfo.FriendlyName);
        _installationFailedTimeout = _stringResource.GetLocalized("WSLInstallationFailedTimeOut", _distributionInfo.FriendlyName);
        _installationSuccessful = _stringResource.GetLocalized("WSLInstallationCompletedSuccessfully", _distributionInfo.FriendlyName);
    }

    public IAsyncOperation<CreateComputeSystemResult> StartAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                var startTime = DateTime.UtcNow;
                _log.Information($"Starting installation for {_distributionInfo.Name}");
                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_preparingToInstall, 0));
                _wslManager.InstallDistribution(_distributionInfo.Name);

                // Cancel waiting for install if the distribution hasn't been installed after 10 minutes.
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(_tenMinuteTimeout);
                WslDistributionInfo? registeredDistribution = null;
                var distributionInstalledSuccessfully = false;

                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_waitingToComplete, 0));
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Wait in 3 second intervals before checking. Unfortunately there are no APIs to check for
                    // installation so we need to keep checking for its completion.
                    await Task.Delay(_threeSecondDelayInSeconds);
                    registeredDistribution = await _wslManager.GetInformationOnRegisteredDistributionAsync(_distributionInfo.Name);

                    if ((registeredDistribution != null) &&
                        (distributionInstalledSuccessfully = registeredDistribution.IsDistributionFullyRegistered()))
                    {
                        break;
                    }
                }

                _log.Information($"Ending installation for {_distributionInfo.Name}. Operation took: {DateTime.UtcNow - startTime}");
                if (distributionInstalledSuccessfully)
                {
                    Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_installationSuccessful, 100));
                    return new CreateComputeSystemResult(new WslRegisteredDistribution(_stringResource, registeredDistribution!, _wslManager));
                }

                throw new TimeoutException(_installationFailedTimeout);
            }
            catch (Exception ex)
            {
                var errorMsg = _stringResource.GetLocalized("WSLInstallationFailedWithException", _distributionInfo.FriendlyName, ex.Message);
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
