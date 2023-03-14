// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ViewModels;

public class AccountsPageViewModel
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly IDevIdProvider? iDevIdProvider;

    public ObservableCollection<AccountViewModel> LoggedInAccounts { get; } = new ();

    public AccountsPageViewModel()
    {
        // Currently, we assume that there is only one extension
        var pluginService = new DevHome.Services.PluginService();
        var plugins = new List<IPluginWrapper>();
        var plugin = plugins.FirstOrDefault();
        if (plugin is null)
        {
            // Nothing to do if there are no plugins
            return;
        }

        if (!plugin.IsRunning())
        {
            _ = plugin.StartPlugin();
        }

        SpinWait.SpinUntil(() => plugin.IsRunning());

        if (plugin.IsRunning())
        {
            var pluginObj = plugin?.GetPluginObject();
            var devIdProvider = pluginObj?.GetProvider(ProviderType.DevId);
            if (devIdProvider is null)
            {
                return;
            }

            iDevIdProvider = devIdProvider as IDevIdProvider;
            if (iDevIdProvider is null)
            {
                return;
            }
        }

        // Currently, we assume there is only 1 DeveloperId
        var devId = iDevIdProvider?.GetLoggedInDeveloperIds()?.FirstOrDefault();
        if (devId is null)
        {
            return;
        }

        LoggedInAccounts.Add(new AccountViewModel(devId));
    }

    public async void AddAccount()
    {
        // Currently, we directly open the browser rather than the AdaptiveCard flyout
        var newDeveloperId = await Task.Run(async () =>
        {
            return await iDevIdProvider?.LoginNewDeveloperIdAsync();
        });
        LoggedInAccounts.Add(new AccountViewModel(newDeveloperId));

        // Bring focus back to DevHome after login
        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    public void RemoveAccount(string loginId)
    {
        var accountToRemove = LoggedInAccounts?.FirstOrDefault(x => x.LoginId == loginId);
        if (accountToRemove != null)
        {
            Task.Run(() => iDevIdProvider?.LogoutDeveloperId(accountToRemove.GetDevId()));
            LoggedInAccounts?.Remove(accountToRemove);
        }
    }
}
