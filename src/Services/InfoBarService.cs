// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.ViewModels;
using Microsoft.UI.Xaml.Controls;
using static DevHome.Common.Services.IInfoBarService;

namespace DevHome.Services;

internal sealed class InfoBarService : IInfoBarService
{
    private readonly InfoBarModel _shellInfoBarModel;

    private PageScope _infoBarScope = PageScope.App;

    public InfoBarService(ShellViewModel shellViewModel)
    {
        _shellInfoBarModel = shellViewModel.ShellInfoBarModel;
    }

    public void HideAppLevelInfoBar() => _shellInfoBarModel.IsOpen = false;

    public bool IsAppLevelInfoBarVisible() => _shellInfoBarModel.IsOpen;

    public void ShowAppLevelInfoBar(InfoBarSeverity infoBarSeverity, string title, string message, bool isClosable, PageScope pageSource = PageScope.App)
    {
        _shellInfoBarModel.Title = title;
        _shellInfoBarModel.Description = message;
        _shellInfoBarModel.Severity = infoBarSeverity;
        _shellInfoBarModel.IsClosable = isClosable;
        _shellInfoBarModel.IsOpen = true;

        _infoBarScope = pageSource;
    }

    public PageScope GetInfoBarPageScope() => _infoBarScope;
}
