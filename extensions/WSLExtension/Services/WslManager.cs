// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.Core.Contracts;
using Serilog;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.System.Threading;
using WSLExtension.Contracts;
using WSLExtension.DistributionDefinitions;
using WSLExtension.Helpers;
using WSLExtension.Models;
using static WSLExtension.Constants;

namespace WSLExtension.Services;

public class WslManager : IWslManager, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslManager));

    private readonly PackageHelper _packageHelper = new();

    private readonly TimeSpan _oneMinutePollingInterval = TimeSpan.FromMinutes(1);

    private readonly TimeSpan _packageInstallTimeOut = TimeSpan.FromMinutes(30);

    private readonly WslRegisteredDistributionFactory _wslRegisteredDistributionFactory;

    private readonly IWslServicesMediator _wslServicesMediator;

    private readonly IDistributionDefinitionHelper _definitionHelper;

    private readonly List<WslComputeSystem> _registeredWslDistributions = new();

    private readonly IMicrosoftStoreService _microsoftStoreService;

    private readonly IStringResource _stringResource;

    private readonly object _distributionInstallLock = new();

    private readonly SemaphoreSlim _wslKernelPackageInstallLock = new(1, 1);

    private readonly HashSet<string> _distributionsBeingInstalled = new();

    public event EventHandler<HashSet<string>>? DistributionStateSyncEventHandler;

    private Dictionary<string, DistributionDefinition>? _distributionDefinitionsMap;

    private ThreadPoolTimer? _timerForUpdatingDistributionStates;

    private bool _disposed;

    public event EventHandler<AppInstallItem>? WslInstallationEventHandler;

    public WslManager(
        IWslServicesMediator wslServicesMediator,
        WslRegisteredDistributionFactory wslDistributionFactory,
        IDistributionDefinitionHelper distributionDefinitionHelper,
        IMicrosoftStoreService microsoftStoreService,
        IStringResource stringResource)
    {
        _wslRegisteredDistributionFactory = wslDistributionFactory;
        _wslServicesMediator = wslServicesMediator;
        _definitionHelper = distributionDefinitionHelper;
        _microsoftStoreService = microsoftStoreService;
        _stringResource = stringResource;
        _microsoftStoreService.ItemStatusChanged += OnInstallChanged;
        StartDistributionStatePolling();
    }

    /// <inheritdoc cref="IWslManager.GetAllRegisteredDistributionsAsync"/>
    public async Task<List<WslComputeSystem>> GetAllRegisteredDistributionsAsync()
    {
        // The list of compute systems in Dev Home is being refreshed, so remove any old
        // subscriptions
        _registeredWslDistributions.ForEach(distribution => distribution.RemoveSubscriptions());
        _registeredWslDistributions.Clear();

        foreach (var distribution in await GetInformationOnAllRegisteredDistributionsAsync())
        {
            try
            {
                _registeredWslDistributions.Add(_wslRegisteredDistributionFactory(distribution.Value));
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to add the distribution: {distribution.Key}");
            }
        }

        return _registeredWslDistributions;
    }

    /// <inheritdoc cref="IWslManager.GetAllDistributionsAvailableToInstallAsync"/>
    public async Task<List<DistributionDefinition>> GetAllDistributionsAvailableToInstallAsync()
    {
        var registeredDistributionsMap = await GetInformationOnAllRegisteredDistributionsAsync();
        var distributionsToListOnCreationPage = new List<DistributionDefinition>();
        _distributionDefinitionsMap ??= await _definitionHelper.GetDistributionDefinitionsAsync();

        lock (_distributionInstallLock)
        {
            foreach (var distributionDefinition in _distributionDefinitionsMap.Values)
            {
                // filter out distribution definitions already registered on machine.
                if (registeredDistributionsMap.TryGetValue(distributionDefinition.Name, out var _))
                {
                    continue;
                }

                // filter out distributions that are currently being installed/registered.
                if (_distributionsBeingInstalled.Contains(distributionDefinition.Name))
                {
                    continue;
                }

                distributionsToListOnCreationPage.Add(distributionDefinition);
            }
        }

        // Sort the list by distribution name in ascending order before sending it.
        distributionsToListOnCreationPage.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return distributionsToListOnCreationPage;
    }

    /// <inheritdoc cref="IWslManager.GetInformationOnRegisteredDistributionAsync"/>
    public async Task<WslRegisteredDistribution?> GetInformationOnRegisteredDistributionAsync(string distributionName)
    {
        foreach (var registeredDistribution in (await GetInformationOnAllRegisteredDistributionsAsync()).Values)
        {
            if (distributionName.Equals(registeredDistribution.Name, StringComparison.Ordinal))
            {
                return registeredDistribution;
            }
        }

        return null;
    }

    /// <inheritdoc cref="IWslManager.IsDistributionRunning"/>
    public bool IsDistributionRunning(string distributionName)
    {
        return _wslServicesMediator.IsDistributionRunning(distributionName);
    }

    /// <inheritdoc cref="IWslManager.UnregisterDistribution"/>
    public void UnregisterDistribution(string distributionName)
    {
        _wslServicesMediator.UnregisterDistribution(distributionName);
    }

    /// <inheritdoc cref="IWslManager.LaunchDistribution"/>
    public void LaunchDistribution(string distributionName)
    {
        _wslServicesMediator.LaunchDistribution(distributionName);
    }

    /// <inheritdoc cref="IWslManager.InstallDistributionPackageAsync"/>
    public async Task InstallDistributionPackageAsync(
        DistributionDefinition definition,
        Action<string>? statusUpdateCallback,
        CancellationToken cancellationToken)
    {
        lock (_distributionInstallLock)
        {
            if (_distributionsBeingInstalled.Contains(definition.Name))
            {
                throw new InvalidOperationException("Distribution already being installed");
            }

            _distributionsBeingInstalled.Add(definition.Name);
        }

        try
        {
            statusUpdateCallback?.Invoke(_stringResource.GetLocalized("DistributionPackageInstallationCheck", definition.FriendlyName));
            if (!_packageHelper.IsPackageInstalled(definition.PackageFamilyName))
            {
                // Install it from the store.
                statusUpdateCallback?.Invoke(_stringResource.GetLocalized("DistributionPackageInstallationStart", definition.FriendlyName));
                cancellationToken.ThrowIfCancellationRequested();
                await _microsoftStoreService.InstallPackageAsync(definition.StoreAppId, _packageInstallTimeOut);
            }
            else
            {
                statusUpdateCallback?.Invoke(_stringResource.GetLocalized("DistributionPackageAlreadyInstalled", definition.FriendlyName));
            }

            var package = _packageHelper.GetPackageFromPackageFamilyName(definition.PackageFamilyName);
            if (package == null)
            {
                throw new InvalidDataException($"Couldn't find the {definition.Name} package");
            }

            statusUpdateCallback?.Invoke(_stringResource.GetLocalized("WSLWaitingToCompleteRegistration", definition.FriendlyName));
            cancellationToken.ThrowIfCancellationRequested();
            _wslServicesMediator.InstallAndRegisterDistribution(package);
            statusUpdateCallback?.Invoke(_stringResource.GetLocalized("WSLRegistrationCompletedSuccessfully", definition.FriendlyName));
        }
        finally
        {
            lock (_distributionInstallLock)
            {
                _distributionsBeingInstalled.Remove(definition.Name);
            }
        }
    }

    /// <inheritdoc cref="IWslManager.TerminateDistribution"/>
    public void TerminateDistribution(string distributionName)
    {
        _wslServicesMediator.TerminateDistribution(distributionName);
    }

    /// <inheritdoc cref="IWslManager.InstallWslKernelPackageAsync"/>
    public async Task InstallWslKernelPackageAsync(Action<string>? statusUpdateCallback, CancellationToken cancellationToken)
    {
        // Regardless of how many WSL distributions are being installed. Only one thread should be allowed to install the
        // WSL kernel package if it isn't already installed.
        await _wslKernelPackageInstallLock.WaitAsync(cancellationToken);
        try
        {
            statusUpdateCallback?.Invoke(_stringResource.GetLocalized("WslKernelPackageInstallationCheck"));
            if (!_packageHelper.IsPackageInstalled(WSLPackageFamilyName))
            {
                // If not installed, we'll install it from the store.
                statusUpdateCallback?.Invoke(_stringResource.GetLocalized("InstallingWslKernelPackage"));
                cancellationToken.ThrowIfCancellationRequested();
                await _microsoftStoreService.InstallPackageAsync(WslKernelPackageStoreId, _packageInstallTimeOut);

                if (!_packageHelper.IsPackageInstalled(WSLPackageFamilyName))
                {
                    throw new InvalidDataException("Failed to install the Wsl kernel package");
                }
            }

            statusUpdateCallback?.Invoke(_stringResource.GetLocalized("WslKernelPackageInstalled"));
        }
        finally
        {
            _wslKernelPackageInstallLock.Release();
        }
    }

    /// <summary>
    /// Retrieves information about all registered distributions on the machine and fills in any missing data
    /// that is needed for them to be shown in Dev Home's UI. E.g logo images.
    /// </summary>
    private async Task<Dictionary<string, WslRegisteredDistribution>> GetInformationOnAllRegisteredDistributionsAsync()
    {
        _distributionDefinitionsMap ??= await _definitionHelper.GetDistributionDefinitionsAsync();
        var distributions = new Dictionary<string, WslRegisteredDistribution>();
        foreach (var distribution in _wslServicesMediator.GetAllRegisteredDistributions())
        {
            // If this is a distribution we know about in DistributionDefinition.yaml add its friendly name and logo.
            if (_distributionDefinitionsMap.TryGetValue(distribution.Name, out var knownDistributionInfo))
            {
                distribution.FriendlyName = knownDistributionInfo.FriendlyName;
                distribution.Base64StringLogo = knownDistributionInfo.Base64StringLogo;
                distribution.AssociatedTerminalProfileGuid = knownDistributionInfo.WindowsTerminalProfileGuid;
            }

            distributions.Add(distribution.Name, distribution);
        }

        return distributions;
    }

    /// <summary>
    /// Raises an event once every minute so that the wsl compute systems state can be updated. Unfortunately there
    /// are no WSL APIs to achieve this. Once an API is created that fires an event for state changes this can be
    /// updated/removed.
    /// </summary>
    private void StartDistributionStatePolling()
    {
        _timerForUpdatingDistributionStates = ThreadPoolTimer.CreatePeriodicTimer(
            (ThreadPoolTimer timer) =>
            {
                try
                {
                    DistributionStateSyncEventHandler?.Invoke(this, _wslServicesMediator.GetAllNamesOfRunningDistributions());
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to raise distribution sync event due to an error");
                }
            },
            _oneMinutePollingInterval);
    }

    private void OnInstallChanged(object sender, AppInstallManagerItemEventArgs args)
    {
        var installItem = args.Item;
        var installationStatus = installItem.GetCurrentStatus();
        var itemInstallState = installationStatus.InstallState;

        lock (_distributionInstallLock)
        {
            if (_distributionsBeingInstalled.Contains(installItem.ProductId))
            {
                if (itemInstallState == AppInstallState.Completed ||
                    itemInstallState == AppInstallState.Canceled ||
                    itemInstallState == AppInstallState.Error)
                {
                    _distributionsBeingInstalled.Remove(installItem.ProductId);
                }
            }
        }

        WslInstallationEventHandler?.Invoke(this, installItem);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _log.Debug("Disposing WslManager");
            if (disposing)
            {
                _wslKernelPackageInstallLock.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
