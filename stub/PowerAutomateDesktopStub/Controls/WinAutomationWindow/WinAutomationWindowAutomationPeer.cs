// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI.Controls.WinAutomationWindow
{
	public class WinAutomationWindowAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
	{
		#region Fields/Consts

		private readonly WinAutomationWindow _owner;

		#endregion

		public WinAutomationWindowAutomationPeer(Window owner)
			: base(owner)
		{
			_owner = (WinAutomationWindow)owner;
		}

		#region IWindowProvider Implementation

		public WindowInteractionState InteractionState
		{
			get
			{
				if (_owner.IsActive) return WindowInteractionState.ReadyForUserInteraction;
				if (_owner.IsLoaded) return WindowInteractionState.Running;

				return WindowInteractionState.Closing;
			}
		}

		public virtual bool IsModal => false;

		public bool IsTopmost => _owner.Topmost;

		public bool Maximizable => _owner.IsMaxButtonEnabled;

		public bool Minimizable => _owner.IsMinButtonEnabled;

		public WindowVisualState VisualState
		{
			get
			{
				switch (_owner.WindowState)
				{
					case WindowState.Normal:    return WindowVisualState.Normal;
					case WindowState.Minimized: return WindowVisualState.Minimized;
					case WindowState.Maximized: return WindowVisualState.Maximized;
					default:                    return WindowVisualState.Normal;
				}
			}
		}

		public void Close()
		{
			_owner.Close();
		}

		public void SetVisualState(WindowVisualState state)
		{
			switch (state)
			{
				case WindowVisualState.Normal:
					_owner.WindowState = WindowState.Normal;
					break;
				case WindowVisualState.Maximized:
					_owner.WindowState = WindowState.Maximized;
					break;
				case WindowVisualState.Minimized:
					_owner.WindowState = WindowState.Minimized;
					break;
			}
		}

		public bool WaitForInputIdle(int milliseconds)
		{
			return true;
		}

		#endregion

		#region Methods Overrides

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Window;
		}

		protected override string GetClassNameCore()
		{
			return nameof(WinAutomationWindow);
		}

		protected override string GetNameCore()
		{
			var name = base.GetNameCore();

			if (string.IsNullOrEmpty(name))
			{
				name = _owner.Title ?? string.Empty;
			}

			return name;
		}

		#endregion
	}
}