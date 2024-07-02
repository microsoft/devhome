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

    private readonly TimeSpan _tenMinuteTimeout = TimeSpan.FromMinutes(10);

    private readonly TimeSpan _threeSecondDelayInSeconds = TimeSpan.FromSeconds(3);

    private readonly DistributionDefinition _definiton;

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public WslInstallDistributionOperation(
        DistributionDefinition distributionDefinition,
        IStringResource stringResource,
        IWslManager wslManager)
    {
        _definiton = distributionDefinition;
        _stringResource = stringResource;
        _wslManager = wslManager;
        _preparingToInstall = GetLocalizedString("WSLPrepareInstall", _definiton.FriendlyName);
        _waitingToComplete = GetLocalizedString("WSLWaitingToCompleteInstallation", _definiton.FriendlyName);

        _installationFailedTimeout = GetLocalizedString("WSLInstallationFailedTimeOut", _definiton.FriendlyName);

        _installationSuccessful = GetLocalizedString("WSLInstallationCompletedSuccessfully", _definiton.FriendlyName);
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
                _log.Information($"Starting installation for {_definiton.Name}");
                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_preparingToInstall, 0));
                _wslManager.InstallDistribution(_definiton.Name);

                // Cancel waiting for install if the distribution hasn't been installed after 10 minutes.
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(_tenMinuteTimeout);
                WslRegisteredDistribution? registeredDistribution = null;
                var distributionInstalledSuccessfully = false;

                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_waitingToComplete, 0));
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Wait in 3 second intervals before checking. Unfortunately there are no APIs to check for
                    // installation so we need to keep checking for its completion.
                    await Task.Delay(_threeSecondDelayInSeconds);
                    registeredDistribution = await _wslManager.GetInformationOnRegisteredDistributionAsync(_definiton.Name);

                    if ((registeredDistribution != null) &&
                        (distributionInstalledSuccessfully = registeredDistribution.IsDistributionFullyRegistered()))
                    {
                        break;
                    }
                }

                _log.Information($"Ending installation for {_definiton.Name}. Operation took: {DateTime.UtcNow - startTime}");
                if (distributionInstalledSuccessfully)
                {
                    Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_installationSuccessful, 100));
                    return new CreateComputeSystemResult(new WslComputeSystem(_stringResource, registeredDistribution!, _wslManager));
                }

                throw new TimeoutException(_installationFailedTimeout);
            }
            catch (Exception ex)
            {
                var errorMsg = _stringResource.GetLocalized("WSLInstallationFailedWithException", _definiton.FriendlyName, ex.Message);
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
