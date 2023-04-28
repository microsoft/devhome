// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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

    public IPluginAdaptiveCardController GetAdaptiveCardController(string[] args)
    {
        if (args.Length > 0 && args[0] == "LoginUI")
        {
            LoggedIn.Invoke(this, null);
            LoggedOut.Invoke(this, null);
            Updated.Invoke(this, null);

            return new LoginUI();
        }

        return null;
    }

    public IEnumerable<IDeveloperId> GetLoggedInDeveloperIds() => throw new NotImplementedException();

    public string GetName() => throw new NotImplementedException();

    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync() => throw new NotImplementedException();

    public void LogoutDeveloperId(IDeveloperId developerId) => throw new NotImplementedException();

    public void SignalDispose() => throw new NotImplementedException();
}
