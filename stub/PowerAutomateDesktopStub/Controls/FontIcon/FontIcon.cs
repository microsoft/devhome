﻿// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.PowerAutomateDesktop.Stub.Controls
{
	public class FontIcon : Control
	{
		#region Fields/Consts

		public static readonly DependencyProperty CodeProperty =
			DependencyProperty.Register("Code", typeof(string), typeof(FontIcon), new PropertyMetadata(null, OnCodeChanged));

		public static readonly DependencyProperty CodeStringProperty =
			DependencyProperty.Register("CodeString", typeof(string), typeof(FontIcon), new PropertyMetadata(null));

		public static readonly DependencyProperty HighContrastForegroundProperty =
			DependencyProperty.Register("HighContrastForeground", typeof(Brush), typeof(FontIcon), new PropertyMetadata(default(Brush)));

		public static readonly DependencyProperty SizeProperty =
			DependencyProperty.Register("Size", typeof(double), typeof(FontIcon), new PropertyMetadata(16d));

		#endregion

		#region Properties

		public string Code
		{
			get => (string)GetValue(CodeProperty);
			set => SetValue(CodeProperty, value);
		}

		public string CodeString
		{
			get => (string)GetValue(CodeStringProperty);
			set => SetValue(CodeStringProperty, value);
		}

		public Brush HighContrastForeground
		{
			get => (Brush)GetValue(HighContrastForegroundProperty);
			set => SetValue(HighContrastForegroundProperty, value);
		}

		public double Size
		{
			get => (double)GetValue(SizeProperty);
			set => SetValue(SizeProperty, value);
		}

		#endregion

		static FontIcon()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(FontIcon), new FrameworkPropertyMetadata(typeof(FontIcon)));
		}

		#region Methods

		private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(e.NewValue is string newValue) || string.IsNullOrEmpty(newValue))
			{
                d.ClearValue(CodeStringProperty);
                return;
			}

			char hexChar;

			try
			{
				hexChar = (char)Int16.Parse(newValue, NumberStyles.AllowHexSpecifier);
			}
			catch (Exception)
			{
                d.ClearValue(CodeStringProperty);
                return;
			}

			d.SetCurrentValue(CodeStringProperty, Char.ToString(hexChar));
		}

		#endregion
	}
}