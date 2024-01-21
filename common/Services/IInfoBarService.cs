// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Services;

public interface IInfoBarService
{
    void ShowAppLevelInfoBar(InfoBarSeverity infoBarSeverity, string title, string message);

    void HideAppLevelInfoBar();

    bool IsAppLevelInfoBarVisible();
}
