// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Windows.UI.ViewManagement;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI
{
	public class FontSizeManager
	{
		#region Fields/Consts

		private static ResourceDictionary _dictionary;
		private static IDictionary<object, double> _defaultValues;
		private static readonly UISettings Settings;

		#endregion

		static FontSizeManager()
		{
			Settings = new UISettings();
		}

		#region Methods

		public static void Register(ResourceDictionary dictionary)
		{
			try
			{
				Settings.TextScaleFactorChanged -= Settings_TextScaleFactorChanged;
			}
			catch (Exception)
			{
				// TO-DO: Log exception
			}

			_dictionary = dictionary;
			_defaultValues = dictionary.Cast<DictionaryEntry>().Where(entry => entry.Value is double)
										.ToDictionary(entry => entry.Key, entry => (double)entry.Value);

			UpdateValues(Settings.TextScaleFactor);

			try
			{
				Settings.TextScaleFactorChanged += Settings_TextScaleFactorChanged;
			}
			catch (Exception)
			{
				// TO-DO: Log exception
			}
		}

		#endregion

		#region Event Subscriptions

		private static void Settings_TextScaleFactorChanged(UISettings sender, object args)
		{
			var dispatcher = Application.Current.Dispatcher;
			if (dispatcher != null && !dispatcher.CheckAccess())
			{
				dispatcher.Invoke(() => UpdateValues(sender.TextScaleFactor));
				return;
			}

			UpdateValues(sender.TextScaleFactor);
		}

		private static void UpdateValues(double scaleFactor)
		{
			foreach (var key in _defaultValues.Keys)
			{
				_dictionary[key] = _defaultValues[key] * scaleFactor;
			}
		}

		#endregion
	}
}