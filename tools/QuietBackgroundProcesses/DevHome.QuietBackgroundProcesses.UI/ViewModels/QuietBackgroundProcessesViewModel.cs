// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Services;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Serilog;
using Windows.Foundation.Diagnostics;

namespace DevHome.QuietBackgroundProcesses.UI.ViewModels;

public partial class QuietBackgroundProcessesViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuietBackgroundProcessesViewModel));

    private readonly IExperimentationService _experimentationService;

    private readonly TimeSpan _zero = new(0, 0, 0);
    private readonly TimeSpan _oneSecond = new(0, 0, 1);
    private readonly DispatcherQueue _dispatcherQueue;
    private TimeSpan _sessionDuration;
    private QuietBackgroundProcessesSession? _session;
    private ProcessPerformanceTable? _table;

    [ObservableProperty]
    private bool _isFeaturePresent;

    [ObservableProperty]
    private bool _isAnalyticSummaryAvailable;

    [ObservableProperty]
    private string _countdownTimer;

    [ObservableProperty]
    private string _sessionStateText;

    [ObservableProperty]
    private SolidColorBrush _sessionStateTextColor;

    [ObservableProperty]
    private bool _quietButtonEnabled;

    [ObservableProperty]
    private bool _quietButtonChecked;

    [ObservableProperty]
    private string? _quietButtonText;

    private QuietBackgroundProcessesSession GetSession()
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

    private void SetErrorString(string id)
    {
        QuietButtonEnabled = false;
        SessionStateText = GetStatusString(id);
        SessionStateTextColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
    }

    public bool IsQuietBackgroundProcessesFeatureEnabled => _experimentationService.IsFeatureEnabled("QuietBackgroundProcessesExperiment");

    public QuietBackgroundProcessesViewModel(
        IExperimentationService experimentationService,
        DispatcherQueue dispatcherQueue)
    {
        _experimentationService = experimentationService;
        _dispatcherQueue = dispatcherQueue;
        _countdownTimer = string.Empty;
        _sessionStateText = GetString("QuietBackgroundProcesses_Description");

        // Set the default text color since there's no way to acquire it from
        // the theme in code (and we want to change it when an error happens).
        _sessionStateTextColor = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        _dispatcherTimer = new DispatcherTimer();
    }

    public async Task LoadViewModelContentAsync()
    {
        await Task.Run(async () =>
        {
            if (!IsQuietBackgroundProcessesFeatureEnabled)
            {
                return;
            }

            var isFeaturePresent = false;
            try
            {
                isFeaturePresent = QuietBackgroundProcessesSessionManager.IsFeaturePresent();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "COM error");
            }

            var isAvailable = false;
            isAvailable = _table != null;
            if (!isAvailable)
            {
                try
                {
                    isAvailable = QuietBackgroundProcessesSessionManager.HasLastPerformanceRecording();
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "COM error");
                }
            }

            var running = false;
            long? timeLeftInSeconds = null;
            if (isFeaturePresent)
            {
                // Check if an existing quiet session is running.
                // Note: GetIsActive() won't ever launch a UAC prompt, but GetTimeRemaining() will if no session is running - so be careful with call order
                running = GetIsActive();
                if (running)
                {
                    timeLeftInSeconds = GetTimeRemaining();
                }
            }

            // Update the UI thread
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                IsFeaturePresent = isFeaturePresent;
                IsAnalyticSummaryAvailable = isAvailable;
                if (IsFeaturePresent)
                {
                    QuietButtonEnabled = true;

                    // Resume countdown if there's an existing quiet window
                    SetQuietSessionRunningState(running, timeLeftInSeconds);
                }
                else
                {
                    SetErrorString("FeatureNotSupported");
                    QuietButtonText = GetString("QuietBackgroundProcesses_QuietButton_Start");
                }
            });
        });
    }

    private void SetQuietSessionRunningState(bool running, long? timeLeftInSeconds = null)
    {
        if (running)
        {
            var seconds = timeLeftInSeconds ?? GetTimeRemaining();
            StartCountdownTimer(seconds);
            QuietButtonText = GetString("QuietBackgroundProcesses_QuietButton_Stop");
            IsAnalyticSummaryAvailable = false;
        }
        else
        {
            _dispatcherTimer?.Stop();
            QuietButtonText = GetString("QuietBackgroundProcesses_QuietButton_Start");
            if (!IsAnalyticSummaryAvailable)
            {
                IsAnalyticSummaryAvailable = QuietBackgroundProcessesSessionManager.HasLastPerformanceRecording();
            }
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
                TelemetryFactory.Get<ITelemetry>().Log("QuietBackgroundProcesses_Session", LogLevel.Critical, new QuietBackgroundProcessesEvent(LoggingOpcode.Start));

                // Launch the server, which then elevates itself, showing a UAC prompt
                var timeLeftInSeconds = GetSession().Start();
                _sessionDuration = TimeSpan.FromSeconds(timeLeftInSeconds);
                SetQuietSessionRunningState(true, timeLeftInSeconds);
            }
            catch (Exception ex)
            {
                TelemetryFactory.Get<ITelemetry>().Log("QuietBackgroundProcesses_SessionStartError", LogLevel.Critical, new QuietBackgroundProcessesEvent());

                SetErrorString("SessionStartError");
                _log.Error(ex, "QuietBackgroundProcessesSession::Start failed");
            }
        }
        else
        {
            try
            {
                TelemetryFactory.Get<ITelemetry>().Log("QuietBackgroundProcesses_Session", LogLevel.Critical, new QuietBackgroundProcessesEvent(LoggingOpcode.Stop));

                _table = GetSession().Stop();
                IsAnalyticSummaryAvailable = _table != null;
                SetQuietSessionRunningState(false);
                CountdownTimer = GetLastSessionLengthString(_sessionDuration - _secondsLeft);
            }
            catch (Exception ex)
            {
                TelemetryFactory.Get<ITelemetry>().Log("QuietBackgroundProcesses_SessionStopError", LogLevel.Critical, new QuietBackgroundProcessesEvent());

                SetErrorString("UnableToCancelSession");
                _log.Error(ex, "QuietBackgroundProcessesSession::Stop failed");
            }
        }
    }

    private bool GetIsActive()
    {
        try
        {
            _session = QuietBackgroundProcessesSessionManager.TryGetSession();
            if (_session != null)
            {
                return _session.IsActive;
            }
        }
        catch (Exception ex)
        {
            SetErrorString("UnableToFindActiveSession");
            _log.Error(ex, "QuietBackgroundProcessesSession::IsActive failed");
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
            SetErrorString("UnableToGetTimeLeft");
            _log.Error(ex, "QuietBackgroundProcessesSession::TimeLeftInSeconds failed");
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

        CountdownTimer = _secondsLeft.ToString();
    }

    private void DispatcherTimer_Tick(object? sender, object e)
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
            var lastSessionLength = _sessionDuration - _secondsLeft;
            _secondsLeft = _zero;
            CountdownTimer = GetLastSessionLengthString(lastSessionLength);
        }
        else
        {
            CountdownTimer = _secondsLeft.ToString(); // CultureInfo.InvariantCulture
        }
    }

    private string GetLastSessionLengthString(TimeSpan lastSessionLength)
    {
        return GetString("QuietBackgroundProcesses_Time_LastSessionLength") + " " + lastSessionLength.ToString("g", CultureInfo.CurrentCulture);
    }

    public ProcessPerformanceTable? GetProcessPerformanceTable()
    {
        if (_table == null)
        {
            try
            {
                _table = QuietBackgroundProcessesSessionManager.TryGetLastPerformanceRecording();
            }
            catch (Exception ex)
            {
                SetErrorString("UnableToGetAnalyticSummary");
                _log.Error(ex, "QuietBackgroundProcessesSessionManager.TryGetLastPerformanceRecording failed");
            }
        }

        return _table;
    }
}
