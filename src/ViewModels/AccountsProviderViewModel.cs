// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly IDevIdProvider _devIdProvider;

    public ObservableCollection<AccountViewModel> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDevIdProvider devIdProvider)
    {
        _devIdProvider = devIdProvider;
        _devIdProvider.GetLoggedInDeveloperIds().ToList().ForEach((devId) =>
        {
            LoggedInAccounts.Add(new AccountViewModel(this, devId));
        });
    }

    public string ProviderName => _devIdProvider.GetName();

    public void AddAccount()
    {
        // Currently, we directly open the browser rather than the AdaptiveCard flyout
        var adaptiveCardController = _devIdProvider.GetAdaptiveCardController(null);
        var loginUI = new IPluginAdaptiveCard();
        adaptiveCardController.Initialize();

        // Only add to LoggedInAccounts if not already present
        if (!LoggedInAccounts.Any((account) => account.LoginId == newDeveloperId.LoginId()))
        {
            LoggedInAccounts.Add(new AccountViewModel(this, newDeveloperId));
        }

        // Refresh LoggedInAccounts list
        _devIdProvider.GetLoggedInDeveloperIds().ToList().ForEach((devId) =>
        {
            LoggedInAccounts.Add(new AccountViewModel(this, devId));
        });

        // Bring focus back to DevHome after login
        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    public void RemoveAccount(string loginId)
    {
        var accountToRemove = LoggedInAccounts.FirstOrDefault(x => x.LoginId == loginId);
        if (accountToRemove != null)
        {
            _devIdProvider.LogoutDeveloperId(accountToRemove.GetDevId());
        }

        // Refresh LoggedInAccounts list
        _devIdProvider.GetLoggedInDeveloperIds().ToList().ForEach((devId) =>
        {
            LoggedInAccounts.Add(new AccountViewModel(this, devId));
        });
    }
}
