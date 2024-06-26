// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using WSLExtension.Common;
using WSLExtension.Services;

namespace WSLExtension.Models;

public class WslInstallAndRegisterDistroOperation : ICreateComputeSystemOperation
{
    private readonly Distro _distro;
    private readonly IStringResource _stringResource;
    private readonly IWslManager _wslManager;

    public WslInstallAndRegisterDistroOperation(Distro distro, IStringResource stringResource, IWslManager wslManager)
    {
        _distro = distro;
        _stringResource = stringResource;
        _wslManager = wslManager;
    }

    public IAsyncOperation<CreateComputeSystemResult> StartAsync()
    {
        return Task.Run(async () =>
        {
            Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs("Installing", 0));

            var registration = _distro.Registration;
            if (!_wslManager.IsWslEnabled)
            {
                await _wslManager.InstallWslDistribution(registration);
                Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs("Installed", 100));

                var d = _wslManager.Definitions
                    .FirstOrDefault(d => d.Registration == _distro.Registration);
                if (d == default)
                {
                    return new CreateComputeSystemResult(new WslRegisteredDistro(_stringResource, _wslManager)
                    {
                        DisplayName = _distro.Name ?? registration,
                        SupplementalDisplayName = _distro.Name != null && _distro.Name != registration ? registration : string.Empty,
                        Id = registration,
                    });
                }

                return new CreateComputeSystemResult(new WslRegisteredDistro(_stringResource, _wslManager)
                {
                    DisplayName = d.Name ?? d.Registration,
                    SupplementalDisplayName = d.Name != null && d.Name != d.Registration ? d.Registration : string.Empty,
                    Running = d.Running,
                    Id = d.Registration,
                    IsDefault = d.DefaultDistro,
                    IsWsl2 = d.Version2,
                    Logo = d.Logo,
                    WtProfileGuid = d.WtProfileGuid,
                });
            }

            _wslManager.InstallWslDistributionDistribution(registration);

            WslRegisteredDistro? foundDistro;
            while ((foundDistro = InstalledDistroRunning(registration)) == default)
            {
                await Task.Delay(1000);
            }

            Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs("Installed", 100));

            return new CreateComputeSystemResult(foundDistro);
        }).AsAsyncOperation();
    }

    private WslRegisteredDistro? InstalledDistroRunning(string registration)
        => _wslManager.GetAllRegisteredDistributions().FirstOrDefault(d => d.Id == registration && d.Running.HasValue && d.Running.Value);

#pragma warning disable CS0067 // The event 'WslInstallAndRegisterDistroOperation.ActionRequired' is never used
    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired;
#pragma warning restore CS0067 // The event 'WslInstallAndRegisterDistroOperation.ActionRequired' is never used

#pragma warning disable CS0067 // The event 'WslInstallAndRegisterDistroOperation.Progress' is never used
    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;
#pragma warning restore CS0067 // The event 'WslInstallAndRegisterDistroOperation.Progress' is never used
}
