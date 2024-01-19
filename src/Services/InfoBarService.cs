// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using DevHome.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Services;
internal class InfoBarService : IInfoBarService
{
    private readonly InfoBarModel _shellInfoBarModel;

    public InfoBarService(ShellViewModel shellViewModel)
    {
        _shellInfoBarModel = shellViewModel.ShellInfoBarModel;
    }

    public void HideAppLevelInfoBar() => _shellInfoBarModel.IsOpen = false;

    public bool IsAppLevelInfoBarVisible() => _shellInfoBarModel.IsOpen;

    public void ShowAppLevelInfoBar(InfoBarSeverity infoBarSeverity, string title, string message)
    {
        _shellInfoBarModel.Title = title;
        _shellInfoBarModel.Description = message;
        _shellInfoBarModel.Severity = infoBarSeverity;
        _shellInfoBarModel.IsOpen = true;
    }
}
