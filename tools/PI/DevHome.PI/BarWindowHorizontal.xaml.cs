// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DevHome.PI.Controls;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.WindowManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI;

public partial class BarWindowHorizontal : WindowEx
{
    private readonly Settings _settings = Settings.Default;
    private readonly string _errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string _errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");
    private readonly string _pinMenuItemText = CommonHelper.GetLocalizedString("PinMenuItemText");
    private readonly string _unpinMenuItemText = CommonHelper.GetLocalizedString("UnpinMenuItemText");
    private readonly BarWindowViewModel _viewModel;

    private ExternalTool? _selectedExternalTool;
    private INotifyCollectionChanged? _externalTools;

    // Constants that control window sizes
    private const int _WindowPositionOffsetY = 30;
    private const int _FloatingHorizontalBarHeight = 70;
    private const int _DefaultExpandedViewTop = 30;
    private const int _DefaultExpandedViewLeft = 100;
    private const int _RightSideGap = 10;

    private RECT _monitorRect;

    private RestoreState _restoreState = new()
    {
        Top = _DefaultExpandedViewTop,
        Left = _DefaultExpandedViewLeft,
        BarOrientation = Orientation.Horizontal,
        IsLargePanelVisible = true,
    };

    private const int _UnsnapGap = 9;
    private double _dpiScale = 1.0;

    internal HWND ThisHwnd { get; private set; }

    internal ClipboardMonitor? ClipboardMonitor { get; private set; }

    public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher
    {
        get; set;
    }

    public BarWindowHorizontal(BarWindowViewModel model)
    {
        _viewModel = model;

        TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

        // Get the default window size. We grab this in the constructor, as
        // we may try and set our window size before our main panel gets
        // loaded (and we call SetDefaultPosition)
        var settingSize = Settings.Default.ExpandedLargeSize;
        _restoreState.Height = settingSize.Height;
        _restoreState.Width = settingSize.Width;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BarWindowViewModel.ShowingExpandedContent))
        {
            if (_viewModel.ShowingExpandedContent)
            {
                ExpandLargeContentPanel();
            }
            else
            {
                CollapseLargeContentPanel();
            }
        }
    }

    private void MainPanel_Loaded(object sender, RoutedEventArgs e)
    {
        ThisHwnd = (HWND)WindowNative.GetWindowHandle(this);

        _settings.PropertyChanged += Settings_PropertyChanged;

        if (_settings.IsClipboardMonitoringEnabled)
        {
            ClipboardMonitor.Instance.Start(ThisHwnd);
        }

        InitializeExternalTools();

        // Apply the user's chosen theme setting.
        ThemeName t = ThemeName.Themes.First(t => t.Name == _settings.CurrentTheme);
        SetRequestedTheme(t.Theme);

        // Calculate the DPI scale.
        var dpiWindow = HwndExtensions.GetDpiForWindow(ThisHwnd);
        _dpiScale = dpiWindow / 96.0;

        SetDefaultPosition();

        SetRegionsForTitleBar();
    }

    public void SetRegionsForTitleBar()
    {
        var scaleAdjustment = MainPanel.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

        var transform = ChromeButtonPanel.TransformToVisual(null);
        var bounds = transform.TransformBounds(new Rect(0, 0, ChromeButtonPanel.ActualWidth, ChromeButtonPanel.ActualHeight));
        Windows.Graphics.RectInt32 chromeButtonsRect = WindowHelper.GetRect(bounds, scaleAdjustment);

        var rectArray = new Windows.Graphics.RectInt32[] { chromeButtonsRect };

        InputNonClientPointerSource nonClientInputSrc =
            InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }

    private void InitializeExternalTools()
    {
        ExternalToolsHelper.Instance.Init();

        ExternalToolsMenu.Items.Clear();
        foreach (var item in ExternalToolsHelper.Instance.AllExternalTools)
        {
            CreateMenuItemFromTool(item);
        }

        // We have to cast to INotifyCollectionChanged explicitly because the CollectionChanged
        // event in ReadOnlyObservableCollection is protected.
        _externalTools = ExternalToolsHelper.Instance.AllExternalTools;
        _externalTools.CollectionChanged += ExternalTools_CollectionChanged;
    }

    private void ExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (ExternalTool item in e.NewItems)
            {
                CreateMenuItemFromTool(item);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (ExternalTool item in e.OldItems)
            {
                var menuItem = ExternalToolsMenu.Items.FirstOrDefault(i => ((ExternalTool)i.Tag).ID == item.ID);
                if (menuItem is not null)
                {
                    ExternalToolsMenu.Items.Remove(menuItem);
                }
            }
        }
    }

    private void CreateMenuItemFromTool(ExternalTool item)
    {
        var imageIcon = new ImageIcon
        {
            Source = item.ToolIcon,
        };

        var menuItem = new MenuFlyoutItem
        {
            Text = item.Name,
            Tag = item,
            Icon = item.MenuIcon,
        };
        menuItem.Click += ExternalToolMenuItem_Click;
        menuItem.RightTapped += ExternalToolMenuItem_RightTapped;
        ExternalToolsMenu.Items.Add(menuItem);

        // You can't databind to MenuFlyoutItem, and the ExternalTool icon image is generated asynchronously,
        // so we'll handle the PropertyChanged event in code, so we can update the icon when it gets set.
        // https://github.com/microsoft/microsoft-ui-xaml/issues/1087
        item.PropertyChanged += ExternalToolItem_PropertyChanged;
    }

    private void ExternalToolItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ExternalTool item && string.Equals(e.PropertyName, nameof(ExternalTool.MenuIcon), StringComparison.Ordinal))
        {
            var menuItem = (MenuFlyoutItem?)ExternalToolsMenu.Items.FirstOrDefault(i => ((ExternalTool)i.Tag).ID == item.ID);
            if (menuItem is not null)
            {
                menuItem.Icon = item.MenuIcon;
            }
        }
    }

    private void ManageExternalToolsButton_Click(object sender, RoutedEventArgs e)
    {
        ExpandLargeContentPanel();
        ExpandedViewControl.NavigateToSettings(typeof(AdditionalToolsViewModel).FullName!);
    }

    private void ExternalToolMenuItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var menuItem = sender as MenuFlyoutItem;
        if (menuItem is not null)
        {
            _selectedExternalTool = (ExternalTool)menuItem.Tag;
            if (_selectedExternalTool.IsPinned)
            {
                PinUnpinMenuItem.Text = _unpinMenuItemText;
            }
            else
            {
                PinUnpinMenuItem.Text = _pinMenuItemText;
            }

            ToolContextMenu.ShowAt(menuItem, e.GetPosition(menuItem));
        }
    }

    private void ExternalToolMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem clickedMenuItem)
        {
            if (clickedMenuItem.Tag is ExternalTool tool)
            {
                InvokeTool(tool, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);
            }
        }
    }

    private void ExternalToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton)
        {
            if (clickedButton.Tag is ExternalTool tool)
            {
                InvokeTool(tool, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);
            }
        }
    }

    private void InvokeTool(ExternalTool tool, int? id, HWND hWnd)
    {
        var process = tool.Invoke(id, hWnd);
        if (process is null)
        {
            // A ContentDialog only renders in the space its parent occupies. Since the parent is a narrow
            // bar, the dialog doesn't have enough space to render. So, we'll use MessageBox to display errors.
            PInvoke.MessageBox(
                ThisHwnd,
                string.Format(CultureInfo.CurrentCulture, _errorMessageText, tool.Executable),
                _errorTitleText,
                MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }

    private void ExternalToolButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Button clickedButton)
        {
            _selectedExternalTool = (ExternalTool)clickedButton.Tag;
            if (_selectedExternalTool.IsPinned)
            {
                PinUnpinMenuItem.Text = _unpinMenuItemText;
            }
            else
            {
                PinUnpinMenuItem.Text = _pinMenuItemText;
            }
        }
    }

    private void PinUnpinMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Pin or unpin the tool on the bar.
        if (_selectedExternalTool is not null)
        {
            _selectedExternalTool.IsPinned = !_selectedExternalTool.IsPinned;
        }
    }

    private void UnregisterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedExternalTool is not null)
        {
            ExternalToolsHelper.Instance.RemoveExternalTool(_selectedExternalTool);
            _selectedExternalTool = null;
        }
    }

    private void SetDefaultPosition()
    {
        _monitorRect = GetMonitorRectForWindow(ThisHwnd);
        var screenWidth = _monitorRect.right - _monitorRect.left;
        this.Move(
            (int)(((screenWidth - Width) / 2) * _dpiScale),
            (int)(_WindowPositionOffsetY * _dpiScale));

        // Get the saved settings for the ExpandedView size. On first run, this will be
        // the default 0,0, so we'll set the size proportional to the monitor size.
        // Subsequently, it will be whatever size the user sets.
        var settingSize = Settings.Default.ExpandedLargeSize;
        if (settingSize.Width == 0)
        {
            settingSize.Width = _monitorRect.Width * 2 / 3;
        }

        if (settingSize.Height == 0)
        {
            settingSize.Height = _monitorRect.Height * 3 / 4;
        }

        Settings.Default.ExpandedLargeSize = settingSize;
        Settings.Default.Save();

        // Set the default restore state for the ExpandedView size to the (adjusted) settings size.
        _restoreState.Height = settingSize.Height;
        _restoreState.Width = settingSize.Width;
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        ClipboardMonitor.Instance.Stop();

        if (LargeContentPanel is not null &&
            LargeContentPanel.Visibility == Visibility.Visible &&
            this.WindowState != WindowState.Maximized)
        {
            CacheRestoreState();
        }
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsClipboardMonitoringEnabled))
        {
            if (_settings.IsClipboardMonitoringEnabled)
            {
                ClipboardMonitor.Instance.Start(ThisHwnd);
            }
            else
            {
                ClipboardMonitor.Instance.Stop();
            }
        }
    }

    internal void SetRequestedTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }

    private void CacheRestoreState()
    {
        _restoreState = new()
        {
            Left = AppWindow.Position.X,
            Top = AppWindow.Position.Y,
            Width = Width,
            Height = Height,
            IsLargePanelVisible = LargeContentPanel.Visibility == Visibility.Visible,
        };

        Settings.Default.ExpandedLargeSize = new System.Drawing.Size((int)Width, (int)Height);
        Settings.Default.Save();
    }

    private void ExpandLargeContentPanel()
    {
        // We're expanding.
        // Switch the bar to horizontal before we adjust the size.
        LargeContentPanel.Visibility = Visibility.Visible;
        MaxHeight = double.NaN;

        // If they expand to ExpandedView and they're not snapped, we can use the
        // RestoreState size & position.
        if (!_viewModel.IsSnapped)
        {
            this.MoveAndResize(
                _restoreState.Left, _restoreState.Top, _restoreState.Width, _restoreState.Height);
        }
        else
        {
            // Conversely if they're snapped, the position is determined by the snap,
            // and we potentially adjust the size to ensure it doesn't extend beyond the screen.
            var availableWidth = _monitorRect.Width - Math.Abs(AppWindow.Position.X) - _RightSideGap;
            if (availableWidth < _restoreState.Width)
            {
                _restoreState.Width = availableWidth;
            }

            Width = _restoreState.Width;

            var availableHeight = _monitorRect.Height - Math.Abs(AppWindow.Position.Y);
            if (availableHeight < _restoreState.Height)
            {
                _restoreState.Height = availableHeight;
            }

            Height = _restoreState.Height;
        }
    }

    private void CollapseLargeContentPanel()
    {
        // Make sure we cache the state before switching to collapsed bar.
        CacheRestoreState();
        LargeContentPanel.Visibility = Visibility.Collapsed;
        MaxHeight = _FloatingHorizontalBarHeight;
    }

    private void ProcessChooserButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowingExpandedContent = true;
        ExpandedViewControl.NavigateTo(typeof(ProcessListPageViewModel));
    }

    internal void NavigateTo(Type viewModelType)
    {
        _viewModel.ShowingExpandedContent = true;
        ExpandedViewControl.NavigateTo(viewModelType);
    }

    internal Frame GetFrame()
    {
        return ExpandedViewControl.GetPageFrame();
    }

    private void MainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetRegionsForTitleBar();
    }
}
