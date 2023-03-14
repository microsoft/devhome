// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI.Helpers
{
	/// <summary>
	///     A helper class that provides various controls.
	/// </summary>
	public static class ControlsHelper
	{
		#region Fields/Consts

		public static readonly DependencyProperty PromptStringProperty =
			DependencyProperty.RegisterAttached("PromptString", typeof(string), typeof(ControlsHelper), new FrameworkPropertyMetadata(null));

		public static readonly DependencyProperty IsKeyboardFocusedWithinProperty =
			DependencyProperty.RegisterAttached("IsKeyboardFocusedWithin", typeof(bool), typeof(ControlsHelper), new PropertyMetadata(false, OnIsKeyboardFocusedWithinChanged));

		public static readonly DependencyProperty FocusOnLoadProperty =
			DependencyProperty.RegisterAttached("FocusOnLoad", typeof(bool), typeof(ControlsHelper), new PropertyMetadata(false, OnFocusOnLoadChanged));

		public static readonly DependencyProperty TriggerFocusProperty =
			DependencyProperty.RegisterAttached("TriggerFocus", typeof(bool), typeof(ControlsHelper), new PropertyMetadata(false, OnTriggerFocusChanged));

		public static readonly DependencyProperty HasVisualFocusProperty =
			DependencyProperty.RegisterAttached("HasVisualFocus", typeof(bool), typeof(ControlsHelper), new PropertyMetadata(false));

		public static readonly DependencyProperty IsMonitoringInputDeviceProperty =
			DependencyProperty.RegisterAttached("IsMonitoringInputDevice", typeof(bool), typeof(ControlsHelper), new FrameworkPropertyMetadata(false, OnIsMonitoringInputDeviceChanged));

		public static readonly DependencyProperty IsContextMenuOpenProperty =
			DependencyProperty.RegisterAttached("IsContextMenuOpen", typeof(bool), typeof(ControlsHelper), new FrameworkPropertyMetadata(false));

		public static readonly DependencyProperty MonitorInputDeviceProperty =
			DependencyProperty.RegisterAttached("MonitorInputDevice", typeof(bool), typeof(ControlsHelper), new PropertyMetadata(false, OnMonitorInputDevice));

		#endregion

		#region Methods

		public static bool GetFocusOnLoad(DependencyObject obj)
		{
			return (bool)obj.GetValue(FocusOnLoadProperty);
		}

		public static bool GetHasVisualFocus(DependencyObject obj)
		{
			return (bool)obj.GetValue(HasVisualFocusProperty);
		}

		public static bool GetIsContextMenuOpen(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsContextMenuOpenProperty);
		}

		[AttachedPropertyBrowsableForType(typeof(IInputElement))]
		public static bool GetIsKeyboardFocusedWithin(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsKeyboardFocusedWithinProperty);
		}

		public static bool GetIsMonitoringInputDevice(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsMonitoringInputDeviceProperty);
		}

		public static bool GetMonitorInputDevice(DependencyObject obj)
		{
			return (bool)obj.GetValue(MonitorInputDeviceProperty);
		}

		public static string GetPromptString(DependencyObject obj)
		{
			return (string)obj.GetValue(PromptStringProperty);
		}

		public static bool GetTriggerFocus(DependencyObject obj)
		{
			return (bool)obj.GetValue(TriggerFocusProperty);
		}

		public static void SetFocusOnLoad(DependencyObject obj, bool value)
		{
			obj.SetValue(FocusOnLoadProperty, value);
		}

		public static void SetHasVisualFocus(DependencyObject obj, bool value)
		{
			obj.SetValue(HasVisualFocusProperty, value);
		}

		public static void SetIsContextMenuOpen(DependencyObject obj, bool value)
		{
			obj.SetValue(IsContextMenuOpenProperty, value);
		}

		public static void SetIsKeyboardFocusedWithin(DependencyObject obj, bool value)
		{
			obj.SetValue(IsKeyboardFocusedWithinProperty, value);
		}

		public static void SetIsMonitoringInputDevice(DependencyObject obj, bool value)
		{
			obj.SetValue(IsMonitoringInputDeviceProperty, value);
		}

		public static void SetMonitorInputDevice(DependencyObject obj, bool value)
		{
			obj.SetValue(MonitorInputDeviceProperty, value);
		}

		public static void SetPromptString(DependencyObject obj, string value)
		{
			obj.SetValue(PromptStringProperty, value);
		}

		public static void SetTriggerFocus(DependencyObject obj, bool value)
		{
			obj.SetValue(TriggerFocusProperty, value);
		}

		private static void OnFocusOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var target = (FrameworkElement)d;

			if (!(bool)e.NewValue)
			{
				target.Loaded -= Target_Loaded;
				return;
			}

			if (target.IsLoaded)
			{
				target.Focus();
				return;
			}

			target.Loaded += Target_Loaded;
		}

		private static void OnIsKeyboardFocusedWithinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				Keyboard.Focus((IInputElement)d);
			}
		}

		private static void OnIsMonitoringInputDeviceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is Control target)) return;

			var enabled = (bool)e.NewValue;

			if (enabled)
			{
				target.GotKeyboardFocus += Target_GotKeyboardFocus;
				target.LostKeyboardFocus += Target_LostKeyboardFocus;
			}
			else
			{
				target.GotKeyboardFocus -= Target_GotKeyboardFocus;
				target.LostKeyboardFocus -= Target_LostKeyboardFocus;
			}
		}

		private static void OnMonitorInputDevice(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is Control target)) return;

			var enabled = (bool)e.NewValue;

			if (enabled)
			{
				var b = new Binding(nameof(Control.IsVisible)) { RelativeSource = RelativeSource.Self };

				target.SetBinding(IsMonitoringInputDeviceProperty, b);
			}
			else
			{
				BindingOperations.ClearBinding(target, IsMonitoringInputDeviceProperty);
			}
		}

		private static void OnTriggerFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue && d is UIElement element)
			{
				element.Focus();
			}
		}

		#endregion

		#region Event Subscriptions

		private static void Target_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (!(e.OriginalSource is Control target)) return;

			var isKeyboardMostRecentInputDevice = InputManager.Current.MostRecentInputDevice is KeyboardDevice;
			SetHasVisualFocus(target, isKeyboardMostRecentInputDevice);
		}

		private static void Target_Loaded(object sender, RoutedEventArgs e)
		{
			if (!(e.OriginalSource is Control target)) return;
			target.Loaded -= Target_Loaded;

			Keyboard.Focus(target);
		}

		private static void Target_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (!(e.OriginalSource is Control target)) return;
			SetHasVisualFocus(target, false);
		}

		#endregion
	}
}