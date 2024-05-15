// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32.Foundation;

namespace DevHome.PI.Controls;

// This is the base class for a system/chrome/title bar. There are at least 2 consumers of this,
// one which implements a system bar in the horizontal position, another in a vertical position
public partial class SystemBar : UserControl, INotifyPropertyChanged
{
    private bool _isSnapped;

    // This is used to toggle the "snap to an app" icon to have different values
    // based on if we are snapped or not
    public bool IsSnapped
    {
        get => _isSnapped;
        set
        {
            _isSnapped = value;
            CurrentSnapButtonText = _isSnapped ? UnsnapButtonText : SnapButtonText;
        }
    }

    private bool _isSnappingEnabled;

    // This is used to enable the "snap to an app" icon if we're allowed to snap
    public bool IsSnappingEnabled
    {
        get => _isSnappingEnabled;
        set
        {
            _isSnappingEnabled = value;
            OnPropertyChanged(nameof(IsSnappingEnabled));
        }
    }

    private bool _isMaximized;

    // This is used determine if the system bar should treat the window as maximized or not. That
    // allows us to enable/disable the "Maximize" button and the "Restore" button in the titlebar
    public bool IsMaximized
    {
        get => _isMaximized;
        set
        {
            _isMaximized = value;
            MaximizeButtonVisibility = _isMaximized ? Visibility.Collapsed : Visibility.Visible;
            RestoreButtonVisibility = _isMaximized ? Visibility.Visible : Visibility.Collapsed;
            OnPropertyChanged(nameof(MaximizeButtonVisibility));
            OnPropertyChanged(nameof(RestoreButtonVisibility));
        }
    }

    protected Visibility MaximizeButtonVisibility { get; private set; } = Visibility.Visible;

    protected Visibility RestoreButtonVisibility { get; private set; } = Visibility.Collapsed;

    private const string UnsnapButtonText = "\ue89f";
    private const string SnapButtonText = "\ue8a0";

    private string _currentSnapButtonText = SnapButtonText;

    protected string CurrentSnapButtonText
    {
        get => _currentSnapButtonText;
        private set
        {
            if (_currentSnapButtonText != value)
            {
                _currentSnapButtonText = value;
                OnPropertyChanged(nameof(CurrentSnapButtonText));
            }
        }
    }

    public SystemBar()
    {
    }

    public void Initialize()
    {
        IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.HWnd))
        {
            IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;
        }
    }

    protected void SnapButton_Click(object sender, RoutedEventArgs e)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.PerformSnapAction();
    }

    protected void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.HandleMinimizeRequest();
    }

    protected void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.HandleMaximizeRequest();
    }

    protected void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.HandleRestoreRequest();
    }

    protected void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.HandleCloseRequest();
    }

    protected void CloseAllMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.HandleCloseAllRequest();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
