// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.QuietBackgroundProcesses.UI.ViewModels;

public partial class QuietBackgroundProcessesViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuietBackgroundProcessesViewModel));

    private readonly TimeSpan _zero = new TimeSpan(0, 0, 0);
    private readonly TimeSpan _oneSecond = new TimeSpan(0, 0, 1);
#nullable enable
    private DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSession? _session;
#nullable disable

    [ObservableProperty]
    private bool _isFeaturePresent;

    [ObservableProperty]
    private string _sessionStateText;

    [ObservableProperty]
    private bool _quietButtonChecked;

    [ObservableProperty]
    private string _quietButtonText;

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
        var stringResource = new StringResource("DevHome.QuietBackgroundProcesses.UI.pri", "DevHome.QuietBackgroundProcesses.UI/Resources");
        return stringResource.GetLocalized(id);
    }

    private string GetStatusString(string id)
    {
        return GetString("QuietBackgroundProcesses_Status_" + id);
    }

    public QuietBackgroundProcessesViewModel()
    {
        IsFeaturePresent = DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSessionManager.IsFeaturePresent();

        var running = false;
        if (IsFeaturePresent)
        {
            // Check if an existing quiet session is running.
            // Note: GetIsActive() won't ever launch a UAC prompt, but GetTimeRemaining() will if no session is running - so be careful with call order
            running = GetIsActive();
        }
        else
        {
            SessionStateText = GetStatusString("FeatureNotSupported");
        }

        // Resume countdown if there's an existing quiet window
        SetQuietSessionRunningState(running);
    }

    private void SetQuietSessionRunningState(bool running, long? timeLeftInSeconds = null)
    {
        if (running)
        {
            var seconds = timeLeftInSeconds ?? GetTimeRemaining();
            StartCountdownTimer(seconds);
            QuietButtonText = GetString("QuietBackgroundProcesses_QuietButton_Stop");
        }
        else
        {
            _dispatcherTimer?.Stop();
            QuietButtonText = GetString("QuietBackgroundProcesses_QuietButton_Start");
        }

        QuietButtonChecked = !running;
    }

    [RelayCommand]
    public void QuietButtonClicked()
    {
        if (QuietButtonChecked)
        {
            try
            {
                // Launch the server, which then elevates itself, showing a UAC prompt
                var timeLeftInSeconds = GetSession().Start();
                SetQuietSessionRunningState(true, timeLeftInSeconds);
            }
            catch (Exception ex)
            {
                SessionStateText = GetStatusString("SessionError");
                _log.Error("QuietBackgroundProcessesSession::Start failed", ex);
            }
        }
        else
        {
            try
            {
                GetSession().Stop();
                SetQuietSessionRunningState(false);
                SessionStateText = GetStatusString("SessionEnded");
            }
            catch (Exception ex)
            {
                SessionStateText = GetStatusString("UnableToCancelSession");
                _log.Error("QuietBackgroundProcessesSession::Stop failed", ex);
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
            _log.Error("QuietBackgroundProcessesSession::IsActive failed", ex);
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
            _log.Error("QuietBackgroundProcessesSession::TimeLeftInSeconds failed", ex);
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
        // Subtract 1 second
        _secondsLeft = _secondsLeft.Subtract(_oneSecond);

        // Every 2 minutes ask the server for the actual time remaining (to resolve any drift)
        if (_secondsLeft.Seconds % 120 == 0)
        {
            _secondsLeft = new TimeSpan(0, 0, GetTimeRemaining());
        }

        var sessionEnded = false;
        if (_secondsLeft.CompareTo(_zero) <= 0)
        {
            // The window should be closed, but let's confirm with the server
            if (!GetSession().IsActive)
            {
                sessionEnded = true;
            }
        }

        if (sessionEnded)
        {
            SetQuietSessionRunningState(false);
            _secondsLeft = _zero;
            SessionStateText = GetStatusString("SessionEnded");
        }
        else
        {
            SessionStateText = _secondsLeft.ToString(); // CultureInfo.InvariantCulture
        }
    }
}
