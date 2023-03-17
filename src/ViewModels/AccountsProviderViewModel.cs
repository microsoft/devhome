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

    public async void AddAccount()
    {
        // Currently, we directly open the browser rather than the AdaptiveCard flyout
        var newDeveloperId = await _devIdProvider.LoginNewDeveloperIdAsync();

        // Only add to LoggedInAccounts if not already present
        if (!LoggedInAccounts.Any((account) => account.LoginId == newDeveloperId.LoginId()))
        {
            LoggedInAccounts.Add(new AccountViewModel(this, newDeveloperId));
        }

        // Bring focus back to DevHome after login
        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    public void RemoveAccount(string loginId)
    {
        var accountToRemove = LoggedInAccounts?.FirstOrDefault(x => x.LoginId == loginId);
        if (accountToRemove != null)
        {
            _devIdProvider.LogoutDeveloperId(accountToRemove.GetDevId());
            LoggedInAccounts?.Remove(accountToRemove);
        }
    }
}
