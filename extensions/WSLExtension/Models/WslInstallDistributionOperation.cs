// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.DistributionDefinitions;
using static HyperVExtension.Helpers.BytesHelper;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public class WslInstallDistributionOperation : ICreateComputeSystemOperation
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslInstallDistributionOperation));

    private readonly string _wslCreationProcessStart;

    private readonly string _waitingToComplete;

    private readonly string _installationFailedTimeout;

    private readonly string _installationSuccessful;

    private const uint IndeterminateProgressPercentage = 0U;

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
        _wslCreationProcessStart = GetLocalizedString("WSLCreationProcessStart", _definition.FriendlyName);
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
        return AsyncInfo.Run(async (cancellationToken) =>
        {
            try
            {
                var startTime = DateTime.UtcNow;
                _log.Information($"Starting installation for {_definition.Name}");

                // Cancel waiting for install if the distribution hasn't been installed after 10 minutes.
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);
                cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(10));
                StatusUpdateCallback(_wslCreationProcessStart);
                _wslManager.WslInstallationEventHandler += OnInstallChanged;

                // Make sure the WSL kernel package is installed before attempting to install the selected distribution.
                await _wslManager.InstallWslKernelPackageAsync(StatusUpdateCallback, cancellationToken);

                _wslManager.InstallDistribution(_definition.Name);
                WslRegisteredDistribution? registeredDistribution = null;
                var distributionInstalledSuccessfully = false;

                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(_waitingToComplete, 0));
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Wait in 3 second intervals before checking. Unfortunately there are no APIs to check for
                    // installation so we need to keep checking for its completion.
                    await Task.Delay(_threeSecondDelayInSeconds, cancellationToken);
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
                _log.Error(ex, $"Unable to install and register {_definition.FriendlyName} due to exception");
                var errorMsg = _stringResource.GetLocalized("WSLInstallationFailedWithException", _definition.FriendlyName, ex.Message);
                return new CreateComputeSystemResult(ex, errorMsg, ex.Message);
            }
            finally
            {
                _wslManager.WslInstallationEventHandler -= OnInstallChanged;
            }
        });
    }

    private void StatusUpdateCallback(string progressText)
    {
        StatusUpdateCallback(progressText, IndeterminateProgressPercentage);
    }

    private void StatusUpdateCallback(string progressText, uint progressPercent)
    {
        try
        {
            Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(progressText, progressPercent));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to provide progress back to Dev Home");
        }
    }

    private void OnInstallChanged(object? sender, AppInstallItem args)
    {
        var packageName = _definition.FriendlyName;

        if (!_definition.StoreAppId.Equals(args.ProductId, StringComparison.OrdinalIgnoreCase))
        {
            // If we're not downloading/installing the wsl distribution with the provided productId
            // then check if the Linux kernel package is being downloaded/installed.
            if (!WslKernelPackageStoreId.Equals(args.ProductId, StringComparison.OrdinalIgnoreCase))
            {
                // The AppInstallItem isn't the selected distribution nor is it the kernel package.
                return;
            }

            packageName = _stringResource.GetLocalized("WslKernelPackageName");
        }

        var status = args.GetCurrentStatus();
        var itemInstallState = status.InstallState;
        var progressText = GetLocalizedString("AppInstallPending", packageName);
        var progressPercent = IndeterminateProgressPercentage;

        switch (itemInstallState)
        {
            case AppInstallState.Pending:
                break;
            case AppInstallState.Starting:
                progressText = GetLocalizedString("AppInstallStarting", packageName);
                break;
            case AppInstallState.Downloading:
                progressText = GetTextForByteTransfer("AppInstallDownloading", packageName, status);
                progressPercent = (uint)status.PercentComplete;
                break;
            case AppInstallState.Installing:
                progressText = GetLocalizedString("AppInstalling", packageName);
                break;
            case AppInstallState.Completed:
                progressText = GetLocalizedString("AppInstallComplete", packageName);
                break;
            case AppInstallState.Canceled:
                progressText = GetLocalizedString("AppInstallCancelled", packageName);
                break;
            case AppInstallState.Paused:
                progressText = GetLocalizedString("AppInstallPaused", packageName);
                break;
            case AppInstallState.Error:
                progressText = GetLocalizedString("AppInstallError", packageName);
                break;
            case AppInstallState.PausedLowBattery:
                progressText = GetLocalizedString("AppInstallPausedLowBattery", packageName);
                break;
            case AppInstallState.PausedWiFiRecommended:
            case AppInstallState.PausedWiFiRequired:
                progressText = GetLocalizedString("AppInstallPausedWiFi", packageName);
                break;
            case AppInstallState.ReadyToDownload:
                progressText = GetLocalizedString("AppInstallReadyToDownload", packageName);
                break;
        }

        StatusUpdateCallback(progressText, progressPercent);
    }

    private string GetTextForByteTransfer(string resourceKey, string packageName, AppInstallStatus status)
    {
        var bytesReceivedSoFar = ConvertBytesToString(status.BytesDownloaded);
        var totalBytesToReceive = ConvertBytesToString(status.DownloadSizeInBytes);
        return _stringResource.GetLocalized(resourceKey, packageName, $"{bytesReceivedSoFar} / {totalBytesToReceive}");
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired
    {
        add { }
        remove { }
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;
}
