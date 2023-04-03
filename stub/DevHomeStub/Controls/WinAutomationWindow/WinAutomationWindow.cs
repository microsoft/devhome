// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

namespace Microsoft.Flow.RPA.Desktop.Shared.UI.Controls.WinAutomationWindow
{
    [TemplatePart(Name = PartMainBorder, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartIcon, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartTitleBar, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartTitleMenu, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartTitle, Type = typeof(ContentControl))]
    [TemplatePart(Name = PartTitleRightPlaceholder, Type = typeof(ContentControl))]
    [TemplatePart(Name = PartButtons, Type = typeof(Panel))]
    [TemplatePart(Name = PartMinButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartMaxButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartCloseButton, Type = typeof(ButtonBase))]
    public class WinAutomationWindow : Window
    {
        public static readonly DependencyProperty IconTemplateProperty =
            DependencyProperty.Register("IconTemplate", typeof(DataTemplate), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty SaveWindowPositionProperty =
            DependencyProperty.Register("SaveWindowPosition", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ShowIconOnTitleBarProperty =
            DependencyProperty.Register("ShowIconOnTitleBar", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowTitleBarProperty =
            DependencyProperty.Register("ShowTitleBar", typeof(bool), typeof(WinAutomationWindow), new FrameworkPropertyMetadata(true, OnShowTitleBarChanged));

        public static readonly DependencyProperty HighContrastProperty =
            DependencyProperty.RegisterAttached("HighContrast", typeof(bool), typeof(WinAutomationWindow), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IsCloseButtonEnabledProperty =
            DependencyProperty.Register("IsCloseButtonEnabled", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty IsMaxButtonEnabledProperty =
            DependencyProperty.Register("IsMaxButtonEnabled", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty IsMinButtonEnabledProperty =
            DependencyProperty.Register("IsMinButtonEnabled", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyPropertyKey IsShadowSupportedPropertyKey =
            DependencyProperty.RegisterReadOnly("IsShadowSupported", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty NonActiveWindowTitleBrushProperty =
            DependencyProperty.Register("NonActiveWindowTitleBrush", typeof(Brush), typeof(WinAutomationWindow), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register("ShowCloseButton", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowMaxRestoreButtonProperty =
            DependencyProperty.Register("ShowMaxRestoreButton", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowMinButtonProperty =
            DependencyProperty.Register("ShowMinButton", typeof(bool), typeof(WinAutomationWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty TitleAlignmentProperty =
            DependencyProperty.Register("TitleAlignment", typeof(HorizontalAlignment), typeof(WinAutomationWindow), new PropertyMetadata(default(HorizontalAlignment)));

        public static readonly DependencyProperty TitlebarHeightProperty =
            DependencyProperty.Register("TitlebarHeight", typeof(double), typeof(WinAutomationWindow), new FrameworkPropertyMetadata(default(double)));

        public static readonly DependencyProperty TitleForegroundProperty =
            DependencyProperty.Register("TitleForeground", typeof(Brush), typeof(WinAutomationWindow), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty TitleMenuProperty =
            DependencyProperty.Register("TitleMenu", typeof(object), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleMenuTemplateProperty =
            DependencyProperty.Register("TitleMenuTemplate", typeof(DataTemplate), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleRightPlaceholderProperty =
            DependencyProperty.Register("TitleRightPlaceholder", typeof(object), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleTemplateProperty =
            DependencyProperty.Register("TitleTemplate", typeof(DataTemplate), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty WindowCloseButtonStyleProperty =
            DependencyProperty.Register("WindowCloseButtonStyle", typeof(Style), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty WindowMaxButtonStyleProperty =
            DependencyProperty.Register("WindowMaxButtonStyle", typeof(Style), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty WindowMinButtonStyleProperty =
            DependencyProperty.Register("WindowMinButtonStyle", typeof(Style), typeof(WinAutomationWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty WindowTitleBorderBrushProperty =
            DependencyProperty.Register("WindowTitleBorderBrush", typeof(Brush), typeof(WinAutomationWindow), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty WindowTitleBrushProperty =
            DependencyProperty.Register("WindowTitleBrush", typeof(Brush), typeof(WinAutomationWindow), new PropertyMetadata(default(Brush)));

        private static readonly DependencyProperty IsShadowSupportedProperty = IsShadowSupportedPropertyKey.DependencyProperty;
        private FrameworkElement _buttons;
        private FrameworkElement _icon;
        private FrameworkElement _mainBorder;
        private ContentControl _title;
        private FrameworkElement _titleBar;
        private FrameworkElement _titleMenu;
        private FrameworkElement _titleRightPlaceholder;

        public DataTemplate IconTemplate
        {
            get => (DataTemplate)GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        public bool IsCloseButtonEnabled
        {
            get => (bool)GetValue(IsCloseButtonEnabledProperty);
            set => SetValue(IsCloseButtonEnabledProperty, value);
        }

        public bool IsMaxButtonEnabled
        {
            get => (bool)GetValue(IsMaxButtonEnabledProperty);
            set => SetValue(IsMaxButtonEnabledProperty, value);
        }

        public bool IsMinButtonEnabled
        {
            get => (bool)GetValue(IsMinButtonEnabledProperty);
            set => SetValue(IsMinButtonEnabledProperty, value);
        }

        public bool IsShadowSupported
        {
            get => (bool)GetValue(IsShadowSupportedProperty);
            private set => SetValue(IsShadowSupportedPropertyKey, value);
        }

        public Brush NonActiveWindowTitleBrush
        {
            get => (Brush)GetValue(NonActiveWindowTitleBrushProperty);
            set => SetValue(NonActiveWindowTitleBrushProperty, value);
        }

        public bool SaveWindowPosition
        {
            get => (bool)GetValue(SaveWindowPositionProperty);
            set => SetValue(SaveWindowPositionProperty, value);
        }

        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        public bool ShowIconOnTitleBar
        {
            get => (bool)GetValue(ShowIconOnTitleBarProperty);
            set => SetValue(ShowIconOnTitleBarProperty, value);
        }

        public bool ShowMaxRestoreButton
        {
            get => (bool)GetValue(ShowMaxRestoreButtonProperty);
            set => SetValue(ShowMaxRestoreButtonProperty, value);
        }

        public bool ShowMinButton
        {
            get => (bool)GetValue(ShowMinButtonProperty);
            set => SetValue(ShowMinButtonProperty, value);
        }

        public bool ShowTitleBar
        {
            get => (bool)GetValue(ShowTitleBarProperty);
            set => SetValue(ShowTitleBarProperty, value);
        }

        public HorizontalAlignment TitleAlignment
        {
            get => (HorizontalAlignment)GetValue(TitleAlignmentProperty);
            set => SetValue(TitleAlignmentProperty, value);
        }

        public double TitlebarHeight
        {
            get => (double)GetValue(TitlebarHeightProperty);
            set => SetValue(TitlebarHeightProperty, value);
        }

        public Brush TitleForeground
        {
            get => (Brush)GetValue(TitleForegroundProperty);
            set => SetValue(TitleForegroundProperty, value);
        }

        public object TitleMenu
        {
            get => GetValue(TitleMenuProperty);
            set => SetValue(TitleMenuProperty, value);
        }

        public DataTemplate TitleMenuTemplate
        {
            get => (DataTemplate)GetValue(TitleMenuTemplateProperty);
            set => SetValue(TitleMenuTemplateProperty, value);
        }

        public object TitleRightPlaceholder
        {
            get => GetValue(TitleRightPlaceholderProperty);
            set => SetValue(TitleRightPlaceholderProperty, value);
        }

        public DataTemplate TitleTemplate
        {
            get => (DataTemplate)GetValue(TitleTemplateProperty);
            set => SetValue(TitleTemplateProperty, value);
        }

        public Style WindowCloseButtonStyle
        {
            get => (Style)GetValue(WindowCloseButtonStyleProperty);
            set => SetValue(WindowCloseButtonStyleProperty, value);
        }

        public Style WindowMaxButtonStyle
        {
            get => (Style)GetValue(WindowMaxButtonStyleProperty);
            set => SetValue(WindowMaxButtonStyleProperty, value);
        }

        public Style WindowMinButtonStyle
        {
            get => (Style)GetValue(WindowMinButtonStyleProperty);
            set => SetValue(WindowMinButtonStyleProperty, value);
        }

        public Brush WindowTitleBorderBrush
        {
            get => (Brush)GetValue(WindowTitleBorderBrushProperty);
            set => SetValue(WindowTitleBorderBrushProperty, value);
        }

        public Brush WindowTitleBrush
        {
            get => (Brush)GetValue(WindowTitleBrushProperty);
            set => SetValue(WindowTitleBrushProperty, value);
        }

        public WinAutomationWindow()
        {
            SourceInitialized += WinAutomationWindow_SourceInitialized; // Order.
            InitializeCommandBindings();
            IsShadowSupported = SystemParameters.DropShadow && SystemParameters.IsGlassEnabled;
            SetHighContrast(this, SystemParameters.HighContrast);
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
        }

        static WinAutomationWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WinAutomationWindow), new FrameworkPropertyMetadata(typeof(WinAutomationWindow)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_icon != null)
            {
                _icon.MouseDown -= Icon_MouseDown;
            }

            if (_titleMenu != null)
            {
                _titleMenu.SizeChanged -= TitleMenu_SizeChanged;
            }

            _mainBorder = (FrameworkElement)GetTemplateChild(PartMainBorder);
            _icon = (FrameworkElement)GetTemplateChild(PartIcon);
            _titleMenu = (FrameworkElement)GetTemplateChild(PartTitleMenu);
            _titleBar = (FrameworkElement)GetTemplateChild(PartTitleBar);
            _title = (ContentControl)GetTemplateChild(PartTitle);
            _buttons = (FrameworkElement)GetTemplateChild(PartButtons);
            _titleRightPlaceholder = (FrameworkElement)GetTemplateChild(PartTitleRightPlaceholder);

            _titleMenu.SizeChanged += TitleMenu_SizeChanged;
            _icon.MouseDown += Icon_MouseDown;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_icon != null)
            {
                _icon.MouseDown -= Icon_MouseDown;
            }

            if (_titleMenu != null)
            {
                _titleMenu.SizeChanged -= TitleMenu_SizeChanged;
            }

            SourceInitialized -= WinAutomationWindow_SourceInitialized;
            SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
            base.OnClosed(e);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WinAutomationWindowAutomationPeer(this);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateTitleLayout();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            UpdateStateMargins();
        }

        public static bool GetHighContrast(DependencyObject obj)
        {
            return (bool)obj.GetValue(HighContrastProperty);
        }

        public static void SetHighContrast(DependencyObject obj, bool value)
        {
            obj.SetValue(HighContrastProperty, value);
        }

        protected void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private static void OnShowTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WinAutomationWindow)d).ShowTitleBarChanged();
        }

        private void InitializeCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseCommandExecuted, CloseCommandCanExecute));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, MinimizeCommandExecuted, MinimizeCommandCanExecute));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, MaximizeCommandExecuted, MaximizeCommandCanExecute));
        }

        private void ShowTitleBarChanged()
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateChrome();
        }

        /// <summary>
        ///     Configures and updates the <see cref="WindowChrome" /> based on <see cref="WinAutomationWindow" /> settings.
        /// </summary>
        /// <param name="invalidateArrange"></param>
        private void UpdateChrome(bool invalidateArrange = false)
        {
            var chrome = new WindowChrome
            {
                CaptionHeight = ShowTitleBar ? TitlebarHeight : 0,
                NonClientFrameEdges = NonClientFrameEdges.None,
                GlassFrameThickness = IsShadowSupported ? new Thickness(1) : default,
                UseAeroCaptionButtons = false,
                CornerRadius = default,
            };

            WindowChrome.SetWindowChrome(this, chrome);
            UpdateStateMargins();

            if (invalidateArrange)
            {
                InvalidateMeasure();
                InvalidateArrange(); // Gracefully resolves black area issue.
            }
        }

        private void UpdateStateMargins()
        {
            if (ResizeMode != ResizeMode.CanResizeWithGrip)
            {
                _mainBorder.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : default;
            }
        }

        /// <summary>
        ///     Updates the title layout in case of Center alignment while taking into account the <see cref="TitleMenu" /> size,
        ///     if any.
        /// </summary>
        private void UpdateTitleLayout()
        {
            if (TitleAlignment != HorizontalAlignment.Center)
            {
                _title.HorizontalAlignment = TitleAlignment;
                return;
            }

            _title.HorizontalAlignment = HorizontalAlignment.Left;

            var menuWidth = TitleMenu == null && TitleMenuTemplate == null ? 0 : _titleMenu.ActualWidth;
            var leftWidth = _icon.ActualWidth + menuWidth;
            var leftOffset = (_titleBar.ActualWidth - _title.ActualWidth) / 2;
            var offset = leftOffset - leftWidth;
            _title.Margin = new Thickness(leftOffset < leftWidth ? 0 : offset, 0, 0, 0);
        }

        private void CloseCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ShowCloseButton && IsCloseButtonEnabled;
        }

        private void CloseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                Close();
            }
        }

        private void MaximizeCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ShowMaxRestoreButton && IsMaxButtonEnabled;
        }

        private void MaximizeCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void MinimizeCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ShowMinButton && IsMinButtonEnabled;
        }

        private void MinimizeCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Dispatcher != null && !Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler<PropertyChangedEventArgs>(SystemParameters_StaticPropertyChanged), sender, e);
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(SystemParameters.HighContrast):
                    SetHighContrast(this, SystemParameters.HighContrast);
                    break;
                case nameof(SystemParameters.WindowResizeBorderThickness):
                case nameof(SystemParameters.IsGlassEnabled):
                    {
                        IsShadowSupported = SystemParameters.DropShadow && SystemParameters.IsGlassEnabled;
                        var chrome = WindowChrome.GetWindowChrome(this);
                        if (chrome == null)
                        {
                            break;
                        }

                        chrome.GlassFrameThickness = IsShadowSupported ? new Thickness(1) : default;
                        break;
                    }
            }
        }

        private void TitleMenu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTitleLayout();
        }

        private void WinAutomationWindow_SourceInitialized(object sender, EventArgs e)
        {
            if (!IsMaxButtonEnabled)
            {
                DwmHelper.DisableMaximize(this);
            }

            UpdateChrome(true);
        }

        // ReSharper disable InconsistentNaming
        private const string PartMainBorder = "PartMainBorder";
        private const string PartButtons = "PartButtons";
        private const string PartCloseButton = "PartCloseButton";
        private const string PartIcon = "PartIcon";
        private const string PartMaxButton = "PartMaxButton";
        private const string PartMinButton = "PartMinButton";
        private const string PartTitle = "PartTitle";
        private const string PartTitleRightPlaceholder = "PartTitleRightPlaceholder";
        private const string PartTitleBar = "PartTitleBar";

        private const string PartTitleMenu = "PartTitleMenu";

        // ReSharper restore InconsistentNaming
    }
}
