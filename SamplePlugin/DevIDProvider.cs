// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace SamplePlugin;

[ComVisible(true)]
[Guid("BEA53870-57BA-4741-B849-DBC8A3A06CC6")]
[ComDefaultInterface(typeof(IDeveloperIdProvider))]
internal class DevIDProvider : IDeveloperIdProvider
{
    public event EventHandler<IDeveloperId> LoggedIn;

    public event EventHandler<IDeveloperId> LoggedOut;

    public event EventHandler<IDeveloperId> Updated;

    public IExtensionAdaptiveCardSession GetLoginAdaptiveCardSession()
    {
        return new LoginUI();
    }

    public IEnumerable<IDeveloperId> GetLoggedInDeveloperIds()
    {
        return ImmutableList.Create(new DeveloperId("user", "http://localhost/"));
    }

    public string GetName()
    {
        return "Sample Login UI";
    }

    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync()
    {
        var devId = new DeveloperId("user", "http://localhost/");
        LoggedIn.Invoke(this, devId);
        return Task.FromResult<IDeveloperId>(devId).AsAsyncOperation();
    }

    public void LogoutDeveloperId(IDeveloperId developerId)
    {
    }

    public void SignalDispose() => throw new NotImplementedException();
    public void Dispose() => throw new NotImplementedException();
}
