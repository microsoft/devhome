// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Activation;
using DevHome.Common.Services;
using DevHome.Settings.ViewModels;
using Windows.ApplicationModel.Activation;

namespace DevHome.Services;

public class ProtocolActivationHandler : ActivationHandler<ProtocolActivatedEventArgs>
{
    private const string SettingsAccountsUri = "settings/accounts";

    private readonly INavigationService _navigationService;

    public ProtocolActivationHandler(INavigationService navigationService)
    {
        this._navigationService = navigationService;
    }

    protected override Task HandleInternalAsync(ProtocolActivatedEventArgs args)
    {
        if (args.Uri.AbsolutePath == SettingsAccountsUri)
        {
            _navigationService.DefaultPage = typeof(AccountsViewModel).FullName!;
            _navigationService.NavigateTo(_navigationService.DefaultPage);
        }

        return Task.CompletedTask;
    }
}
