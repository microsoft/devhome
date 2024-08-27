// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class RestartDialog : ContentDialog
{
    public RestartDialog()
    {
        this.InitializeComponent();
    }

    private void OnRestartClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        RestartHelper.RestartComputer();
    }
}
