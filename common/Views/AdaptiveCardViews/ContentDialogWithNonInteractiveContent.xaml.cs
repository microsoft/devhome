// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views.AdaptiveCardViews;

public sealed partial class ContentDialogWithNonInteractiveContent : ContentDialog
{
    private readonly StringResource _stringResource = new("DevHome.Common/Resources");

    public ContentDialogWithNonInteractiveContent(StackPanel contentDialogContent, string title, string primaryButtonText, string secondaryButtonText)
    {
        this.InitializeComponent();

        if (string.IsNullOrEmpty(title))
        {
            this.Title = _stringResource.GetLocalized("AdaptiveCardDialogTitleErrorText");
        }
        else
        {
            this.Title = title;
        }

        if (string.IsNullOrEmpty(primaryButtonText))
        {
            this.PrimaryButtonText = _stringResource.GetLocalized("AdaptiveCardDialogPrimaryButtonText");
        }
        else
        {
            this.PrimaryButtonText = primaryButtonText;
        }

        if (string.IsNullOrEmpty(secondaryButtonText))
        {
            this.SecondaryButtonText = _stringResource.GetLocalized("AdaptiveCardDialogSecondaryButtonText");
        }
        else
        {
            this.SecondaryButtonText = secondaryButtonText;
        }

        this.Content = contentDialogContent;
    }
}
