// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Microsoft.PowerAutomateDesktop.Stub.Controls
{
	/// <summary>
	///     A metrofied ProgressBar.
	///     <see cref="ProgressBar" />
	/// </summary>
	public class MetroProgressBar : ProgressBar
	{
		public static readonly DependencyProperty EllipseDiameterProperty
			= DependencyProperty.Register(nameof(EllipseDiameter),
				typeof(double),
				typeof(MetroProgressBar),
				new PropertyMetadata(default(double)));

		public static readonly DependencyProperty EllipseOffsetProperty =
			DependencyProperty.Register(nameof(EllipseOffset),
				typeof(double),
				typeof(MetroProgressBar),
				new PropertyMetadata(default(double)));

		private readonly object lockme = new object();
		private Storyboard indeterminateStoryboard;

		static MetroProgressBar()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroProgressBar), new FrameworkPropertyMetadata(typeof(MetroProgressBar)));
			IsIndeterminateProperty.OverrideMetadata(typeof(MetroProgressBar), new FrameworkPropertyMetadata(OnIsIndeterminateChanged));
		}

		public MetroProgressBar()
		{
			IsVisibleChanged += VisibleChangedHandler;
		}

		/// <summary>
		///     Gets/sets the diameter of the ellipses used in the indeterminate animation.
		/// </summary>
		public double EllipseDiameter
		{
			get => (double)GetValue(EllipseDiameterProperty);
			set => SetValue(EllipseDiameterProperty, value);
		}

		/// <summary>
		///     Gets/sets the offset of the ellipses used in the indeterminate animation.
		/// </summary>
		public double EllipseOffset
		{
			get => (double)GetValue(EllipseOffsetProperty);
			set => SetValue(EllipseOffsetProperty, value);
		}

		private void VisibleChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
		{
			// reset Storyboard if Visibility is set to Visible #1300
			if (IsIndeterminate) ToggleIndeterminate(this, (bool)e.OldValue, (bool)e.NewValue);
		}

		private static void OnIsIndeterminateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			var bar = (MetroProgressBar)dependencyObject;
			if (bar.IsLoaded && bar.IsVisible) ToggleIndeterminate(bar, (bool)e.OldValue, (bool)e.NewValue);
		}

		private static void ToggleIndeterminate(MetroProgressBar bar, bool oldValue, bool newValue)
		{
			if (newValue == oldValue) return;

			var indeterminateState = bar.GetIndeterminate();
			var containingObject = bar.GetTemplateChild("ContainingGrid") as FrameworkElement;

			if (indeterminateState != null && containingObject != null)
			{
				var resetAction = new Action(() =>
				{
					if (oldValue && indeterminateState.Storyboard != null)
					{
						// remove the previous storyboard from the Grid #1855
						indeterminateState.Storyboard.Stop(containingObject);
						indeterminateState.Storyboard.Remove(containingObject);
					}

					if (newValue) bar.ResetStoryboard(bar.ActualSize(true), false);
				});

				bar.Dispatcher?.Invoke(resetAction);
			}
		}

		private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
		{
			var size = ActualSize(false);
			var bar = this;
			if (Visibility == Visibility.Visible && IsIndeterminate) bar.ResetStoryboard(size, true);
		}

		private double ActualSize(bool invalidateMeasureArrange)
		{
			if (invalidateMeasureArrange)
			{
				UpdateLayout();
				Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				InvalidateArrange();
			}

			return Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight;
		}

		private void ResetStoryboard(double width, bool removeOldStoryboard)
		{
			if (!IsIndeterminate) return;

			lock (lockme)
			{
				//perform calculations
				var containerAnimStart = CalcContainerAnimStart(width);
				var containerAnimEnd = CalcContainerAnimEnd(width);
				var ellipseAnimWell = CalcEllipseAnimWell(width);
				var ellipseAnimEnd = CalcEllipseAnimEnd(width);

				//reset the main double animation
				try
				{
					var indeterminate = GetIndeterminate();

					if (indeterminate != null && indeterminateStoryboard != null)
					{
						var newStoryboard = indeterminateStoryboard.Clone();
						var doubleAnim = newStoryboard.Children.First(t => t.Name == "MainDoubleAnim");
						doubleAnim.SetValue(DoubleAnimation.FromProperty, containerAnimStart);
						doubleAnim.SetValue(DoubleAnimation.ToProperty, containerAnimEnd);

						var namesOfElements = new[] {"E1", "E2", "E3", "E4", "E5"};

						foreach (var elemName in namesOfElements)
						{
							var doubleAnimParent = (DoubleAnimationUsingKeyFrames)newStoryboard.Children.First(t => t.Name == elemName + "Anim");
							DoubleKeyFrame first,
											second,
											third;

							if (elemName == "E1")
							{
								first = doubleAnimParent.KeyFrames[1];
								second = doubleAnimParent.KeyFrames[2];
								third = doubleAnimParent.KeyFrames[3];
							}
							else
							{
								first = doubleAnimParent.KeyFrames[2];
								second = doubleAnimParent.KeyFrames[3];
								third = doubleAnimParent.KeyFrames[4];
							}

							first.Value = ellipseAnimWell;
							second.Value = ellipseAnimWell;
							third.Value = ellipseAnimEnd;
							first.InvalidateProperty(DoubleKeyFrame.ValueProperty);
							second.InvalidateProperty(DoubleKeyFrame.ValueProperty);
							third.InvalidateProperty(DoubleKeyFrame.ValueProperty);

							doubleAnimParent.InvalidateProperty(Storyboard.TargetPropertyProperty);
							doubleAnimParent.InvalidateProperty(Storyboard.TargetNameProperty);
						}

						var containingGrid = (FrameworkElement)GetTemplateChild("ContainingGrid");

						if (removeOldStoryboard && indeterminate.Storyboard != null)
						{
							// remove the previous storyboard from the Grid #1855
							indeterminate.Storyboard.Stop(containingGrid);
							indeterminate.Storyboard.Remove(containingGrid);
						}

						indeterminate.Storyboard = newStoryboard;

						indeterminate.Storyboard?.Begin(containingGrid, true);
					}
				}
				catch (Exception)
				{
					//we just ignore 
				}
			}
		}

		private VisualState GetIndeterminate()
		{
			var templateGrid = GetTemplateChild("ContainingGrid") as FrameworkElement;

			if (templateGrid == null)
			{
				ApplyTemplate();
				templateGrid = GetTemplateChild("ContainingGrid") as FrameworkElement;
				if (templateGrid == null) return null;
			}

			var groups = VisualStateManager.GetVisualStateGroups(templateGrid);
			return groups?.OfType<VisualStateGroup>()
						.SelectMany(group => group.States.OfType<VisualState>())
						.FirstOrDefault(state => state.Name == "Indeterminate");
		}

		private void SetEllipseDiameter(double width)
		{
			SetCurrentValue(EllipseDiameterProperty, width <= 180
				? 4d
				: width <= 280
					? 5d
					: 6d);
		}

		private void SetEllipseOffset(double width)
		{
			SetCurrentValue(EllipseOffsetProperty, width <= 180
				? 4d
				: width <= 280
					? 7d
					: 9d);
		}

		private double CalcContainerAnimStart(double width)
		{
			return width <= 180
				? -34
				: width <= 280
					? -50.5
					: -63;
		}

		private double CalcContainerAnimEnd(double width)
		{
			var firstPart = 0.4352 * width;
			return width <= 180
				? firstPart - 25.731
				: width <= 280
					? firstPart + 27.84
					: firstPart + 58.862;
		}

		private double CalcEllipseAnimWell(double width)
		{
			return width * 1.0 / 3.0;
		}

		private double CalcEllipseAnimEnd(double width)
		{
			return width * 2.0 / 3.0;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			lock (lockme)
			{
				indeterminateStoryboard = TryFindResource("IndeterminateStoryboard") as Storyboard;
			}

			Loaded -= LoadedHandler;
			Loaded += LoadedHandler;
		}

		private void LoadedHandler(object sender, RoutedEventArgs routedEventArgs)
		{
			Loaded -= LoadedHandler;
			SizeChangedHandler(null, null);
			SizeChanged += SizeChangedHandler;
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateEllipseProperties();
		}

		private void UpdateEllipseProperties()
		{
			// Update the Ellipse properties to their default values
			// only if they haven't been user-set.
			var actualSize = ActualSize(true);

			if (actualSize > 0)
			{
				if (EllipseDiameter.Equals(0)) SetEllipseDiameter(actualSize);

				if (EllipseOffset.Equals(0)) SetEllipseOffset(actualSize);
			}
		}
	}
}