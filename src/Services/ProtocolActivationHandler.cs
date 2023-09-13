// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
