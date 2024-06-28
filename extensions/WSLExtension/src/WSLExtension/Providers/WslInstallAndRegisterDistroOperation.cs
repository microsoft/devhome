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

            _wslManager.InstallDistribution(registration);

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

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired
    {
        add { }
        remove { }
    }

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;
}
