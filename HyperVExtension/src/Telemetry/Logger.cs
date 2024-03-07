// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Win32;

namespace HyperVExtension.Telemetry;

public class Logger : ILogger
{
    private const string ProviderName = "Microsoft.HyperVExtension";

    /// <summary>
    /// Time Taken Event Name
    /// </summary>
    private const string TimeTakenEventName = "TimeTaken";

    /// <summary>
    /// Exception Thrown Event Name
    /// </summary>
    private const string ExceptionThrownEventName = "ExceptionThrown";

    private static readonly Guid DefaultRelatedActivityId = Guid.Empty;

    /// <summary>
    /// Can only have one EventSource alive per process, so just create one statically.
    /// </summary>
    private static readonly EventSource TelemetryEventSourceInstance = new TelemetryEventSource(ProviderName);

    /// <summary>
    /// Logs telemetry locally, but shouldn't upload it.  Similar to an ETW event.
    /// Should be the same as EventSourceOptions(), as Verbose is the default level.
    /// </summary>
    private static readonly EventSourceOptions LocalOption = new() { Level = EventLevel.Verbose };

    /// <summary>
    /// Logs error telemetry locally, but shouldn't upload it.  Similar to an ETW event.
    /// </summary>
    private static readonly EventSourceOptions LocalErrorOption = new() { Level = EventLevel.Error };

    /// <summary>
    /// Logs telemetry.
    /// Currently this is at 0% sampling for both internal and external retail devices.
    /// </summary>
    private static readonly EventSourceOptions InfoOption = new() { Keywords = TelemetryEventSource.TelemetryKeyword };

    /// <summary>
    /// Logs error telemetry.
    /// Currently this is at 0% sampling for both internal and external retail devices.
    /// </summary>
    private static readonly EventSourceOptions InfoErrorOption = new() { Level = EventLevel.Error, Keywords = TelemetryEventSource.TelemetryKeyword };

    /// <summary>
    /// Logs measure telemetry.
    /// This should be sent back on internal devices, and a small, sampled % of external retail devices.
    /// </summary>
    private static readonly EventSourceOptions MeasureOption = new() { Keywords = TelemetryEventSource.MeasuresKeyword };

    /// <summary>
    /// Logs measure error telemetry.
    /// This should be sent back on internal devices, and a small, sampled % of external retail devices.
    /// </summary>
    private static readonly EventSourceOptions MeasureErrorOption = new() { Level = EventLevel.Error, Keywords = TelemetryEventSource.MeasuresKeyword };

    /// <summary>
    /// Logs critical telemetry.
    /// This should be sent back on all devices sampled at 100%.
    /// </summary>
    private static readonly EventSourceOptions CriticalDataOption = new() { Keywords = TelemetryEventSource.CriticalDataKeyword };

    /// <summary>
    /// Logs critical error telemetry.
    /// This should be sent back on all devices sampled at 100%.
    /// </summary>
    private static readonly EventSourceOptions CriticalDataErrorOption = new() { Level = EventLevel.Error, Keywords = TelemetryEventSource.CriticalDataKeyword };

    /// <summary>
    /// ActivityId so we can correlate all events in the same run
    /// </summary>
    private static Guid activityId = Guid.NewGuid();

    /// <summary>
    /// List of strings we should try removing for sensitivity reasons.
    /// </summary>
    private readonly List<KeyValuePair<string, string>> sensitiveStrings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class.
    /// Prevents a default instance of the Logger class from being created.
    /// </summary>
    internal Logger()
    {
    }

    /// <summary>
    /// Gets a value indicating whether telemetry is on
    /// For future use if we add a registry key or some other setting to check if telemetry is turned on.
    public bool IsTelemetryOn => true;

    /// <summary>
    /// Add a string that we should try stripping out of some of our telemetry for sensitivity reasons (ex. VM name, etc.).
    /// We can never be 100% sure we can remove every string, but this should greatly reduce us collecting PII.
    /// Note that the order in which AddSensitive is called matters, as later when we call ReplaceSensitiveStrings, it will try
    /// finding and replacing the earlier strings first.  This can be helpful, since we can target specific
    /// strings (like username) first, which should help preserve more information helpful for diagnosis.
    /// </summary>
    /// <param name="name">Sensitive string to add (ex. "c:\xyz")</param>
    /// <param name="replaceWith">string to replace it with (ex. "-path-")</param>
    public void AddSensitiveString(string name, string replaceWith)
    {
        // Make sure the name isn't blank, hasn't already been added, and is greater than three characters.
        // Otherwise they could name their VM "a", and then we would end up replacing every "a" with another string.
        if (!string.IsNullOrWhiteSpace(name) && name.Length > 3 && !sensitiveStrings.Exists(item => name.Equals(item.Key, StringComparison.Ordinal)))
        {
            sensitiveStrings.Add(new KeyValuePair<string, string>(name, replaceWith ?? string.Empty));
        }
    }

    /// <summary>
    /// Logs an exception at Measure level. To log at Critical level, the event name needs approval.
    /// </summary>
    /// <param name="action">What we trying to do when the exception occurred.</param>
    /// <param name="e">Exception object</param>
    /// <param name="relatedActivityId">Optional relatedActivityId which will allow to correlate this telemetry with other telemetry in the same action/activity or thread and corelate them</param>
    public void LogException(string action, Exception e, Guid? relatedActivityId = null)
    {
        var innerMessage = ReplaceSensitiveStrings(e.InnerException?.Message);
        var innerStackTrace = new StringBuilder();
        var innerException = e.InnerException;
        while (innerException != null)
        {
            innerStackTrace.Append(innerException.StackTrace);

            // Separating by 2 new lines to distinguish between different exceptions.
            innerStackTrace.AppendLine();
            innerStackTrace.AppendLine();
            innerException = innerException.InnerException;
        }

        LogError(
            ExceptionThrownEventName,
            LogLevel.Measure,
            new
            {
                action,
                name = e.GetType().Name,
                stackTrace = e.StackTrace,
                innerName = e.InnerException?.GetType().Name,
                innerMessage,
                innerStackTrace = innerStackTrace.ToString(),
                message = ReplaceSensitiveStrings(e.Message),
            },
            relatedActivityId ?? DefaultRelatedActivityId);
    }

    /// <summary>
    /// Log the time an action took (ex. deploy time).
    /// </summary>
    /// <param name="eventName">The measurement we're performing (ex. "DeployTime").</param>
    /// <param name="timeTakenMilliseconds">How long the action took in milliseconds.</param>
    /// <param name="relatedActivityId">Optional relatedActivityId which will allow to correlate this telemetry with other telemetry in the same action/activity or thread and corelate them</param>
    public void LogTimeTaken(string eventName, uint timeTakenMilliseconds, Guid? relatedActivityId = null)
    {
        Log(
            TimeTakenEventName,
            LogLevel.Critical,
            new
            {
                eventName,
                timeTakenMilliseconds,
            },
            relatedActivityId ?? DefaultRelatedActivityId);
    }

    /// <summary>
    /// Log an informational event. Typically used for just a single event that's only called one place in the code.
    /// </summary>
    /// <param name="eventName">Name of the error event</param>
    /// <param name="level">Determines whether to upload the data to our servers, and on how many machines.</param>
    /// <param name="data">Values to send to the telemetry system.</param>
    /// <param name="relatedActivityId">Optional relatedActivityId which will allow to correlate this telemetry with other telemetry in the same action/activity or thread and corelate them</param>
    /// <typeparam name="T">Anonymous type.</typeparam>
    public void Log<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string eventName, LogLevel level, T data, Guid? relatedActivityId = null)
    {
        WriteTelemetryEvent(eventName, level, relatedActivityId ?? DefaultRelatedActivityId, false, data);
    }

    /// <summary>
    /// Log an error event. Typically used for just a single event that's only called one place in the code.
    /// </summary>
    /// <param name="eventName">Name of the error event</param>
    /// <param name="level">Determines whether to upload the data to our servers, and on how many machines.</param>
    /// <param name="data">Values to send to the telemetry system.</param>
    /// <param name="relatedActivityId">Optional Optional relatedActivityId which will allow to correlate this telemetry with other telemetry in the same action/activity or thread and corelate them</param>
    /// <typeparam name="T">Anonymous type.</typeparam>
    public void LogError<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string eventName, LogLevel level, T data, Guid? relatedActivityId = null)
    {
        WriteTelemetryEvent(eventName, level, relatedActivityId ?? DefaultRelatedActivityId, true, data);
    }

    /// <summary>
    /// Replaces sensitive strings in a string with non sensitive strings.
    /// </summary>
    /// <param name="message">Before, unstripped string.</param>
    /// <returns>After, stripped string</returns>
    private string ReplaceSensitiveStrings(string message)
    {
        if (message != null)
        {
            foreach (var pair in sensitiveStrings)
            {
                // There's no String.Replace() with case insensitivity.
                // We could use Regular Expressions here for searching for case-insensitive string matches,
                // but it's not easy to specify the RegEx timeout value for .net 4.0.  And we were worried
                // about rare cases where the user could accidentally lock us up with RegEx, since we're using strings
                // provided by the user, so just use a simple non-RegEx replacement algorithm instead.
                var sb = new StringBuilder();
                var i = 0;
                while (true)
                {
                    // Find the string to strip out.
                    var foundPosition = message.IndexOf(pair.Key, i, StringComparison.OrdinalIgnoreCase);
                    if (foundPosition < 0)
                    {
                        sb.Append(message, i, message.Length - i);
                        message = sb.ToString();
                        break;
                    }

                    // Replace the string.
                    sb.Append(message, i, foundPosition - i);
                    sb.Append(pair.Value);
                    i = foundPosition + pair.Key.Length;
                }
            }
        }

        return message;
    }

    /// <summary>
    /// Writes the telemetry event info using the TraceLogging API.
    /// </summary>
    /// <typeparam name="T">Anonymous type.</typeparam>
    /// <param name="eventName">Name of the event.</param>
    /// <param name="level">Determines whether to upload the data to our servers, and the sample set of host machines.</param>
    /// <param name="isError">Set to true if an error condition raised this event.</param>
    /// <param name="data">Values to send to the telemetry system.</param>
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2026:RequiresUnreferencedCode",
        Justification = "The type passed for data is an anonymous type consisting of primitive type properties declared in an assembly that is not marked trimmable.")]
    private void WriteTelemetryEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string eventName, LogLevel level, Guid relatedActivityId, bool isError, T data)
    {
        EventSourceOptions telemetryOptions;
        if (IsTelemetryOn)
        {
            telemetryOptions = level switch
            {
                LogLevel.Critical => isError ? Logger.CriticalDataErrorOption : Logger.CriticalDataOption,
                LogLevel.Measure => isError ? Logger.MeasureErrorOption : Logger.MeasureOption,
                LogLevel.Info => isError ? Logger.InfoErrorOption : Logger.InfoOption,
                _ => isError ? Logger.LocalErrorOption : Logger.LocalOption,
            };
        }
        else
        {
            // The telemetry is not turned on, downgrade to local telemetry
            telemetryOptions = isError ? Logger.LocalErrorOption : Logger.LocalOption;
        }

        TelemetryEventSourceInstance.Write(eventName, ref telemetryOptions, ref activityId, ref relatedActivityId, ref data);
    }

    internal void AddWellKnownSensitiveStrings()
    {
        try
        {
            // This should convert "c:\users\johndoe" to "<SpecialFolder>".
            var userDirectory = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            AddSensitiveString(Directory.GetParent(userDirectory).ToString(), "<SpecialFolder>");

            // Include both these names, since they should cover the logged on user, and the user who is running the tools built on top of these API's
            // These names should almost always be the same, but technically could be different.
            AddSensitiveString(Environment.UserName, "<UserName>");
            var currentUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
            AddSensitiveString(currentUserName, "<CurrentUserName>");
        }
        catch (Exception e)
        {
            // Catch and log exception
            LogException("AddSensitiveStrings", e);
        }
    }
}
