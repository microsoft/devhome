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

internal class DevIDProvider : IDevIdProvider
{
    public event EventHandler<IDeveloperId> LoggedIn;

    public event EventHandler<IDeveloperId> LoggedOut;

    public event EventHandler<IDeveloperId> Updated;

    public IEnumerable<IDeveloperId> GetLoggedInDeveloperIds() => throw new NotImplementedException();

    public string GetName() => "Sample Dev ID Provider";

    public IPluginAdaptiveCardController GetAdaptiveCardController(string[] args)
    {
        if (args.Length > 0 && args[0] == "LoginUI")
        {
            return new LoginUI();
        }

        return null;
    }

    public void LoginNewDeveloperId() => throw new NotImplementedException();

    public void LogoutDeveloperId(IDeveloperId developerId) => throw new NotImplementedException();

    public string LogoutUI() => throw new NotImplementedException();

    IAsyncOperation<IDeveloperId> IDevIdProvider.LoginNewDeveloperIdAsync() => throw new NotImplementedException();
}
