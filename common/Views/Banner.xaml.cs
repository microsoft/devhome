// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

public sealed partial class Banner : UserControl
{
    /// <summary>
    /// Gets or sets the title to display on the Banner.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the description to display on the Banner.
    /// </summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum width of the Title and Description text.
    /// </summary>
    /// <remarks>
    /// This value should be set so that the text does not overlap with the Overlay image.
    /// </remarks>
    public int TextWidth
    {
        get => (int)GetValue(TextWidthProperty);
        set => SetValue(TextWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the path to the image resource used as the background image on the banner.
    /// </summary>
    /// <remarks>
    /// This image will be scaled to the maximum size of the Banner. It is expected to have content for the entire size
    /// of the banner.
    /// </remarks>
    public string BackgroundSource
    {
        get => (string)GetValue(BackgroundSourceProperty);
        set => SetValue(BackgroundSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the path to the image resource used as the overlaid image on the banner.
    /// </summary>
    /// <remarks>
    /// This image will be scaled to the maximum size of the Banner. It is expected that the left side will not have
    /// content, as to not overlap the Banner text. It should also have a transparent background, to let the Background
    /// image show through.
    /// </remarks>
    public string OverlaySource
    {
        get => (string)GetValue(OverlaySourceProperty);
        set => SetValue(OverlaySourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the text displayed on the Banner's action button.
    /// </summary>
    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that will be executed when the Banner's action button is invoked.
    /// </summary>
    public ICommand ButtonCommand
    {
        get => (ICommand)GetValue(ButtonCommandProperty);
        set => SetValue(ButtonCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Button's hide button is shown.
    /// </summary>
    public bool HideButtonVisibility
    {
        get => (bool)GetValue(HideButtonVisibilityProperty);
        set => SetValue(HideButtonVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that will be executed when the Banner's hide button is invoked.
    /// </summary>
    public ICommand HideButtonCommand
    {
        get => (ICommand)GetValue(HideButtonCommandProperty);
        set => SetValue(HideButtonCommandProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Banner"/> class.
    /// </summary>
    /// <remarks>
    /// Use this control to put a banner at the top of your Dev Home page. It can optionally be dismissed and not shown
    /// again on subsequent launches.
    /// </remarks>
    public Banner()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register(nameof(TextWidth), typeof(int), typeof(Banner), new PropertyMetadata(400));
    public static readonly DependencyProperty BackgroundSourceProperty = DependencyProperty.Register(nameof(BackgroundSource), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty OverlaySourceProperty = DependencyProperty.Register(nameof(OverlaySource), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register(nameof(ButtonCommand), typeof(ICommand), typeof(Banner), new PropertyMetadata(null));
    public static readonly DependencyProperty HideButtonVisibilityProperty = DependencyProperty.Register(nameof(HideButtonVisibility), typeof(bool), typeof(Banner), new PropertyMetadata(true));
    public static readonly DependencyProperty HideButtonCommandProperty = DependencyProperty.Register(nameof(HideButtonCommand), typeof(ICommand), typeof(Banner), new PropertyMetadata(null));

    // If the text and button don't fit in the default height of banner, stretch the banner vertically.
    // Set height this way rather than setting MinHeight on the banner, since the banner image assets
    // are taller than the default height and would stretch the banner regardless of the text.
    private void PanelTextContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var defaultBannerHeight = 214;
        BannerGrid.Height = (e.NewSize.Height <= defaultBannerHeight) ? defaultBannerHeight : e.NewSize.Height;
    }
}
