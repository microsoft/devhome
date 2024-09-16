// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class RestartDialog : ContentDialog
{
    public RestartDialog()
    {
        this.InitializeComponent();

        RequestedTheme = Application.Current.GetService<IThemeSelectorService>().Theme;
    }

    private void OnRestartClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        RestartHelper.RestartComputer();
    }
}
