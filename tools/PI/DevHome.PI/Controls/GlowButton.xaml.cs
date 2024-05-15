// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Input;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace DevHome.PI.Controls;

public sealed partial class GlowButton : UserControl
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable SA1401 // Fields should be private
    public static DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(GlowButton), new PropertyMetadata(string.Empty));
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore CA2211 // Non-constant fields should not be visible

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(GlowButton), new PropertyMetadata(null));

    private readonly Compositor compositor;
    private readonly ContainerVisual buttonVisual;
    private readonly ScalarKeyFrameAnimation opacityAnimation;

    public GlowButton()
    {
        InitializeComponent();
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        buttonVisual = (ContainerVisual)ElementCompositionPreview.GetElementVisual(this);

        var result = RegisterPropertyChangedCallback(VisibilityProperty, VisibilityChanged);
        opacityAnimation = CreatePulseAnimation("Opacity", 0.4f, 1.0f, TimeSpan.FromSeconds(5));
    }

    private ScalarKeyFrameAnimation CreatePulseAnimation(string property, float from, float to, TimeSpan duration)
    {
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0.0f, from);
        animation.InsertKeyFrame(0.1f, to);
        animation.InsertKeyFrame(0.3f, from);
        animation.InsertKeyFrame(0.4f, to);
        animation.InsertKeyFrame(0.6f, from);
        animation.InsertKeyFrame(0.7f, to);
        animation.InsertKeyFrame(0.8f, from);
        animation.InsertKeyFrame(0.9f, to);
        animation.Duration = duration;
        animation.Target = property;
        return animation;
    }

    private void VisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (Visibility == Visibility.Visible)
        {
            buttonVisual.StartAnimation("Opacity", opacityAnimation);
        }
    }
}
