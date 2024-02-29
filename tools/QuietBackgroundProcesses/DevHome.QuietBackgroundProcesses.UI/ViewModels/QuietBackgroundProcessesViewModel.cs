// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.QuietBackgroundProcesses;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.Win32;

namespace DevHome.QuietBackgroundProcesses.UI.ViewModels;

public partial class QuietBackgroundProcessesViewModel : ObservableObject
{
    private readonly bool _isFeatureSupported;
    private readonly TimeSpan _zero;
#nullable enable
    private DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSession? _session;
#nullable disable

    [ObservableProperty]
    private string _sessionStateText;

    private DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSession GetSession()
    {
        if (_session == null)
        {
            _session = QuietBackgroundProcessesSessionManager.GetSession();
        }

        return _session;
    }

    private string GetString(string id)
    {
        var stringResource = new StringResource("DevHome.QuietBackgroundProcesses.UI/Resources");
        return stringResource.GetLocalized(id);
    }

    private string GetStatusString(string id)
    {
        return GetString("QuietBackgroundProcesses_Status_" + id);
    }

    public QuietBackgroundProcessesViewModel()
    {
        _zero = new TimeSpan(0, 0, 0);

        _isFeatureSupported = DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSessionManager.IsFeatureSupported();
        if (!_isFeatureSupported)
        {
            SessionStateText = GetStatusString("FeatureNotSupported");
            return;
        }

        // Resume countdown if there's an existing quiet window
        //
        // Note: GetIsActive() won't ever launch a UAC prompt, but GetTimeRemaining() will if no session is running - so be careful with call order
        if (GetIsActive())
        {
            _isToggleOn = true;
            var timeLeftInSeconds = GetTimeRemaining();
            StartCountdownTimer(timeLeftInSeconds);
        }
    }

    public bool IsToggleEnabled => _isFeatureSupported;

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
                    // Launch the server, which then elevates itself, showing a UAC prompt
                    var timeLeftInSeconds = GetSession().Start();
                    StartCountdownTimer(timeLeftInSeconds);
                }
                catch (Exception ex)
                {
                    SessionStateText = GetStatusString("SessionError");
                    Log.Logger()?.ReportError("QuietBackgroundProcessesSession::Start failed", ex);
                }
            }
            else
            {
                try
                {
                    GetSession().Stop();
                    SessionStateText = GetStatusString("SessionEnded");
                }
                catch (Exception ex)
                {
                    SessionStateText = GetStatusString("UnableToCancelSession");
                    Log.Logger()?.ReportError("QuietBackgroundProcessesSession::Stop failed", ex);
                }
            }
        }
    }

    private bool GetIsActive()
    {
        try
        {
            _session = DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSessionManager.TryGetSession();
            if (_session != null)
            {
                return _session.IsActive;
            }
        }
        catch (Exception ex)
        {
            SessionStateText = GetStatusString("SessionError");
            Log.Logger()?.ReportError("QuietBackgroundProcessesSession::IsActive failed", ex);
        }

        return false;
    }

    private int GetTimeRemaining()
    {
        try
        {
            return (int)GetSession().TimeLeftInSeconds;
        }
        catch (Exception ex)
        {
            SessionStateText = GetStatusString("SessionError");
            Log.Logger()?.ReportError("QuietBackgroundProcessesSession::TimeLeftInSeconds failed", ex);
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

        SessionStateText = _secondsLeft.ToString();
    }

    private void DispatcherTimer_Tick(object sender, object e)
    {
        var sessionEnded = false;

        _secondsLeft = new TimeSpan(0, 0, GetTimeRemaining());

        if (_secondsLeft.CompareTo(_zero) <= 0)
        {
            // The window should be closed, but let's confirm with the server
            if (GetSession().IsActive)
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
            SessionStateText = GetStatusString("SessionEnded");
        }
        else
        {
            SessionStateText = _secondsLeft.ToString(); // CultureInfo.InvariantCulture
        }
    }
}
