// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Windows;
using Microsoft.Flow.RPA.Desktop.Shared.UI.Controls.WinAutomationWindow;

namespace Microsoft.PowerAutomateDesktop.Stub
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : WinAutomationWindow
	{
		#region fields

		private readonly MainViewModel _viewModel;

		#endregion

		public MainWindow()
		{
			var protocolHandlerArguments = Application.Current.Properties.Contains("protocolArgs") ? Application.Current.Properties["protocolArgs"].ToString() : default;
			DataContext = _viewModel = new MainViewModel(protocolHandlerArguments);
			Loaded += OnLoaded;

			InitializeComponent();
		}

		private void OnLoaded(object sender, EventArgs e)
		{
			Loaded -= OnLoaded; // First load only.

			_viewModel.Initialize();
		}
	}
}