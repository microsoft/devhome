// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using WSLExtension.Common;
using WSLExtension.Services;

namespace WSLExtension.Models;

public class WslInstallAndRegisterDistroOperation : ICreateComputeSystemOperation
{
    private const double DistributionInstallTimeoutInMinutes = 10;
    private const double ThreeSecondDelayInSeconds = 3;
    private readonly DistributionState _distributionState;
    private readonly IStringResource _stringResource;
    private readonly IWslManager _wslManager;

    public WslInstallAndRegisterDistroOperation(DistributionState distributionState, IStringResource stringResource, IWslManager wslManager)
    {
        _distributionState = distributionState;
        _stringResource = stringResource;
        _wslManager = wslManager;
    }

    public IAsyncOperation<CreateComputeSystemResult> StartAsync()
    {
        return Task.Run(async () =>
        {
            Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs("Installing", 0));

            _wslManager.InstallDistribution(_distributionState.DistributionName);

            // Cancel waiting for install if the distribution hasn't been installed after 10 minutes.
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(DistributionInstallTimeoutInMinutes));
            DistributionState? installedDistribution = null;
            var distributionInstalledSuccessfully = false;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(ThreeSecondDelayInSeconds));
                installedDistribution = await _wslManager.GetRegisteredDistributionAsync(_distributionState.DistributionName);

                if ((installedDistribution != null) && (distributionInstalledSuccessfully = installedDistribution.IsDistributionInstalled()))
                {
                    break;
                }
            }

            if (distributionInstalledSuccessfully)
            {
                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs("Installed", 100));
                return new CreateComputeSystemResult(new WslRegisteredDistribution(_stringResource, installedDistribution!, _wslManager));
            }

            return new CreateComputeSystemResult(new InvalidDataException(), "failed to install new distro", "failed to install new distro");
        }).AsAsyncOperation();
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired
    {
        add { }
        remove { }
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;
}
