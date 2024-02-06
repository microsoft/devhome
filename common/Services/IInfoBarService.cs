// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Services;

public interface IInfoBarService
{
    void ShowAppLevelInfoBar(InfoBarSeverity infoBarSeverity, string title, string message);

    void HideAppLevelInfoBar();

    bool IsAppLevelInfoBarVisible();
}
