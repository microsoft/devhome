// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Services;

public interface IInfoBarService
{
    public enum PageScope
    {
        App,
        MachineConfiguration,
        Environments,
        ExperimentalFeatures,
    }

    void ShowAppLevelInfoBar(InfoBarSeverity infoBarSeverity, string title, string message, bool isClosable, PageScope pageScope = PageScope.App);

    void HideAppLevelInfoBar();

    bool IsAppLevelInfoBarVisible();

    PageScope GetInfoBarPageScope();
}
