// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.Common.Behaviors;
public class TextBlockAutomationBehavior : Behavior<TextBlock>
{
    private long _textChangedToken;

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

    public bool RaiseOnLiveRegionChanged
    {
        get => (bool)GetValue(RaiseOnLiveRegionChangedProperty);
        set => SetValue(RaiseOnLiveRegionChangedProperty, value);
    }

    public AutomationLiveSetting LiveSetting
    {
        get => (AutomationLiveSetting)GetValue(LiveSettingProperty);
        set => SetValue(LiveSettingProperty, value);
    }

    public static readonly DependencyProperty RaiseOnLiveRegionChangedProperty = DependencyProperty.Register(nameof(RaiseOnLiveRegionChanged), typeof(bool), typeof(TextBlockAutomationBehavior), new PropertyMetadata(false));
    public static readonly DependencyProperty LiveSettingProperty = DependencyProperty.Register(nameof(LiveSetting), typeof(AutomationLiveSetting), typeof(TextBlockAutomationBehavior), new PropertyMetadata(AutomationLiveSetting.Assertive));

    private void OnTextChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (RaiseOnLiveRegionChanged)
        {
            Peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
