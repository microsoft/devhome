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

    protected override bool CanHandleInternal(ProtocolActivatedEventArgs args)
    {
        return args.Uri != null && args.Uri.AbsolutePath.Equals(SettingsAccountsUri, StringComparison.OrdinalIgnoreCase);
    }

    protected override Task HandleInternalAsync(ProtocolActivatedEventArgs args)
    {
        _navigationService.NavigateTo(typeof(AccountsViewModel).FullName!);
        return Task.CompletedTask;
    }
}
