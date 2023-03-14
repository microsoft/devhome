// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI
{
	public class ThemedResourceDictionary : ResourceDictionary
	{
		#region Fields/Consts

		private ThemedResourceDictionaryCollection _themeDictionaries;
		private string _currentTheme = "Default";

		#endregion

		#region Properties

		public ThemedResourceDictionaryCollection ThemeDictionaries
		{
			get
			{
				if (_themeDictionaries != null) return _themeDictionaries;

				_themeDictionaries = new ThemedResourceDictionaryCollection();
				_themeDictionaries.CollectionChanged += OnThemeDictionariesChanged;

				return _themeDictionaries;
			}
		}

		#endregion

		public ThemedResourceDictionary()
		{
			SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
		}

		#region Methods

		private bool ChangeTheme(string oldThemeName, string newThemeName)
		{
			//if (oldThemeName == newThemeName) return false;

			if (ThemeDictionaries.Contains(oldThemeName))
			{
				var oldTheme = (ResourceDictionary)ThemeDictionaries[oldThemeName];
				MergedDictionaries.Remove(oldTheme);
			}

			if (!ThemeDictionaries.Contains(newThemeName)) return false;

			var newTheme = (ResourceDictionary)ThemeDictionaries[newThemeName];
			MergedDictionaries.Add(newTheme.Clone());
			_currentTheme = newThemeName;

			return true;
		}

		#endregion

		#region Event Subscriptions

		private void OnThemeDictionariesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					var newDictionary = e.NewItems.OfType<ResourceDictionary>().FirstOrDefault();
					var key = (string)ThemeDictionaries.FindKey(newDictionary);

					if (key == "Default" && !SystemParameters.HighContrast || key == nameof(SystemParameters.HighContrast) && SystemParameters.HighContrast)
					{
						MergedDictionaries.Add(newDictionary);
					}

					break;
				case NotifyCollectionChangedAction.Remove:
					var oldDictionary = e.OldItems.OfType<ResourceDictionary>().FirstOrDefault();
					MergedDictionaries.Remove(oldDictionary);
					break;
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
					oldDictionary = e.OldItems.OfType<ResourceDictionary>().FirstOrDefault();
					newDictionary = e.NewItems.OfType<ResourceDictionary>().FirstOrDefault();
					MergedDictionaries.Remove(oldDictionary);
					MergedDictionaries.Insert(e.NewStartingIndex, newDictionary);
					break;
				case NotifyCollectionChangedAction.Reset:
					MergedDictionaries.Clear();
					break;
			}
		}

		private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(SystemParameters.HighContrast)) return;

			if (!Application.Current.Dispatcher.CheckAccess())
			{
				Application.Current.Dispatcher.Invoke(new EventHandler<PropertyChangedEventArgs>(SystemParameters_StaticPropertyChanged), sender, e);
				return;
			}

			ChangeTheme(_currentTheme, SystemParameters.HighContrast ? nameof(SystemParameters.HighContrast) : "Default");
		}

		#endregion
	}
}