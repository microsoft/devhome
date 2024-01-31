// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;

namespace DevHome.Experiments.ViewModels;
public class QuietBackgroundProcessesViewModel : INotifyPropertyChanged
{
    private readonly TimeSpan _zero;
    private readonly bool _isElevated;
    private readonly bool _validOsVersion;

    public QuietBackgroundProcessesViewModel()
    {
        _zero = new TimeSpan(0, 0, 0);

        var osVersion = Environment.OSVersion;
        _validOsVersion = osVersion.Version.Build >= 26024;
        _validOsVersion = true;

        if (!_validOsVersion)
        {
            TimeLeft = "This feature requires OS version 10.0.26024.0+";
            return;
        }

        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            _isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Resume countdown if there's an existing quiet window
        if (GetIsActive())
        {
            _isToggleOn = true;
            var timeLeftInSeconds = GetTimeRemaining();
            StartCountdownTimer(timeLeftInSeconds);
        }
        else
        {
            if (!_isElevated)
            {
                TimeLeft = "This feature requires running as admin";
            }
        }
    }

    public bool IsToggleEnabled
    {
        get
        {
            return _isElevated && _validOsVersion;
        }
    }

    private bool _isToggleOn;

    public bool IsToggleOn
    {
        get => _isToggleOn;

        set
        {
            if (_isToggleOn == value)
            {
                return;
            }

            _isToggleOn = value;

            // Stop any existing timer
            _dispatcherTimer?.Stop();

            if (_isToggleOn)
            {
                try
                {
                    var timeLeftInSeconds = DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesManager.Start();
                    StartCountdownTimer(timeLeftInSeconds);
                }
                catch
                {
                    TimeLeft = "Service error";
                }
            }
            else
            {
                try
                {
                    DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesManager.Stop();
                    TimeLeft = "Session ended";
                }
                catch
                {
                    TimeLeft = "Unable to cancel session";
                }
            }

            OnPropertyChanged(nameof(IsToggleOn));
        }
    }

    private bool GetIsActive()
    {
        try
        {
            return DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesManager.IsActive;
        }
        catch (Exception ex)
        {
            TimeLeft = "Session error";
            Log.Logger()?.ReportInfo("QuietBackgroundProcesses", $"IsActive = {ex.ToString()}");
            return false;
        }
    }

    private int GetTimeRemaining()
    {
        try
        {
            return (int)DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesManager.TimeLeftInSeconds;
        }
        catch (Exception ex)
        {
            TimeLeft = "Session error";
            Log.Logger()?.ReportInfo("QuietBackgroundProcesses", $"TimeLeftInSeconds = {ex.ToString()}");
            return 0;
        }
    }

    private DispatcherTimer _dispatcherTimer;
    private TimeSpan _secondsLeft;

    private void StartCountdownTimer(long timeLeftInSeconds)
    {
        if (timeLeftInSeconds <= 0)
        {
            return;
        }

        _dispatcherTimer = new DispatcherTimer();
        _dispatcherTimer.Tick += DispatcherTimer_Tick;
        _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        _secondsLeft = new TimeSpan(0, 0, (int)timeLeftInSeconds);
        _dispatcherTimer.Start();

        TimeLeft = _secondsLeft.ToString();
    }

    private void DispatcherTimer_Tick(object sender, object e)
    {
        var sessionEnded = false;

        _secondsLeft = new TimeSpan(0, 0, GetTimeRemaining());

        if (_secondsLeft.CompareTo(_zero) <= 0)
        {
            // The window should be closed, but let's confirm with the server
            if (GetIsActive())
            {
                // There has been some drift
                _secondsLeft = new TimeSpan(0, 0, GetTimeRemaining());
            }
            else
            {
                _dispatcherTimer.Stop();
                _secondsLeft = _zero;
                IsToggleOn = false;
                sessionEnded = true;
            }
        }

        if (sessionEnded)
        {
            TimeLeft = "Session ended";
        }
        else
        {
            TimeLeft = _secondsLeft.ToString(); // CultureInfo.InvariantCulture
        }
    }

    private string _timeLeft = string.Empty;

    public string TimeLeft
    {
        get => _timeLeft;

        set
        {
            _timeLeft = value;
            OnPropertyChanged(nameof(TimeLeft));
        }
    }

    // INotifyPropertyChanged members
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        if (handler != null)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
