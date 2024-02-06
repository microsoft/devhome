// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.Common.Behaviors;

/// <summary>
/// Behavior class for <see cref="TextBlock"/> automation
/// </summary>
public class TextBlockAutomationBehavior : Behavior<TextBlock>
{
    /// <summary>
    /// Callback token for the event handler <see cref="OnTextChanged"/>
    /// </summary>
    private long _textChangedToken;

    public bool RaiseLiveRegionChangedEvent
    {
        get => (bool)GetValue(RaiseLiveRegionChangedEventProperty);
        set => SetValue(RaiseLiveRegionChangedEventProperty, value);
    }

    public AutomationLiveSetting LiveSetting
    {
        get => (AutomationLiveSetting)GetValue(LiveSettingProperty);
        set => SetValue(LiveSettingProperty, value);
    }

    /// <summary>
    /// Gets the automation peer for the associated <see cref="TextBlock"/>
    /// </summary>
    private AutomationPeer Peer => FrameworkElementAutomationPeer.FromElement(AssociatedObject);

    protected override void OnAttached()
    {
        // Register text changed event handler
        _textChangedToken = AssociatedObject.RegisterPropertyChangedCallback(TextBlock.TextProperty, OnTextChanged);

        // Configure automation
        AutomationProperties.SetLiveSetting(AssociatedObject, LiveSetting);

        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.UnregisterPropertyChangedCallback(TextBlock.TextProperty, _textChangedToken);

        base.OnDetaching();
    }

    // Dependency property registration
    public static readonly DependencyProperty RaiseLiveRegionChangedEventProperty = DependencyProperty.Register(nameof(RaiseLiveRegionChangedEvent), typeof(bool), typeof(TextBlockAutomationBehavior), new PropertyMetadata(false));
    public static readonly DependencyProperty LiveSettingProperty = DependencyProperty.Register(nameof(LiveSetting), typeof(AutomationLiveSetting), typeof(TextBlockAutomationBehavior), new PropertyMetadata(AutomationLiveSetting.Assertive));

    private void OnTextChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (RaiseLiveRegionChangedEvent)
        {
            Peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
