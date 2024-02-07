// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        _navigationService.NavigateTo(App.NavConfig.NavMenu.Groups[0].Tools[0].ViewModelFullName, args.Arguments);
        await Task.CompletedTask;
    }
}
