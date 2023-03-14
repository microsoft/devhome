// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI
{
	public class ThemedResourceDictionaryCollection : ObservableCollection<ResourceDictionary>, IDictionary, INameScope
	{
		#region Fields/Consts

		private readonly Dictionary<object, ResourceDictionary> _internalDictionary = new Dictionary<object, ResourceDictionary>();

		#endregion

		#region Properties

		public object this[object key]
		{
			get => _internalDictionary[key];
			set => _internalDictionary[key] = (ResourceDictionary)value;
		}

		#endregion

		#region IDictionary Implementation

		public bool IsFixedSize => false;

		public bool IsReadOnly => false;

		public ICollection Keys => _internalDictionary.Keys;

		public ICollection Values => _internalDictionary.Values;

		public void Add(object key, object value)
		{
			if (!(value is ResourceDictionary resourceDictionary)) return;

			_internalDictionary.Add(key, resourceDictionary);
			base.Add(resourceDictionary);
		}

		public bool Contains(object key)
		{
			return _internalDictionary.ContainsKey(key);
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return _internalDictionary.GetEnumerator();
		}

		public void Remove(object key)
		{
			_internalDictionary.Remove(key);
		}

		#endregion

		#region INameScope Implementation

		public object FindName(string name)
		{
			throw null;
		}

		public void RegisterName(string name, object scopedElement)
		{
			throw new NotSupportedException();
		}

		public void UnregisterName(string name)
		{
		}

		#endregion

		#region Methods

		public object FindKey(ResourceDictionary resourceDictionary)
		{
			return _internalDictionary.FirstOrDefault(kvp => kvp.Value == resourceDictionary).Key;
		}

		#endregion
	}
}