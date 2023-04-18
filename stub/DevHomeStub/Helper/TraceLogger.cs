// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.Versioning;
using DevHome.Telemetry;
using Windows.Foundation.Diagnostics;

namespace DevHome.Stub.Helper;

[SupportedOSPlatform("Windows10.0.21200.0")]
internal class TraceLogger
{
    private const string EventNameResponsive = "InstallControlResponsive";
    private const string EventNameStateChanged = "StateChanged";
    private const string EventNameActivationArgumentsProcessed = "ActivationArgumentsProcessed";
    private const string EventNameUpgradeError = "UpgradeError";
    private const string EventNameAcquiringPackages = "AcquiringPackages";
    private const string EventNameStartSilentUpdate = "StartingSilentUpdate";
    private const string EventNameStartInteractiveUpdate = "StartingInteractiveUpdate";

    private const string PDTPrivacyDataTag = "PartA_PrivTags";
    private const uint PDTProductAandServiceUsage = 0x0000_0000_0200_0000u;

    // c.f. WINEVENT_KEYWORD_RESERVED_63-56 0xFF00000000000000 // Bits 63-56 - channel keywords
    // c.f. WINEVENT_KEYWORD_*              0x00FF000000000000 // Bits 55-48 - system-reserved keywords
    private const long MicrosoftKeywordCritical = 0x0000800000000000; // Bit 47
    private const long MicrosoftKeywordMeasures = 0x0000400000000000; // Bit 46
    private const long MicrosoftKeywordTelemetry = 0x0000200000000000; // Bit 45

    private readonly ILogger logger;

    public static TraceLogger Instance { get; } = new TraceLogger();

    public TraceLogger()
    {
        this.logger = LoggerFactory.Get<ILogger>();
    }

    public void LogCritical(string eventName, LoggingFields fields)
    {
        this.logger.Log(eventName, LogLevel.Critical, fields);
    }

    public void LogMeasures(string eventName, LoggingFields fields)
    {
        this.logger.Log(eventName, LogLevel.Measure, fields);
    }

    public void LogTelemetry(string eventName, LoggingFields fields)
    {
        this.logger.Log(eventName, LogLevel.Info, fields);
    }

    public void LogResponsive()
    {
        var fields = new LoggingFields();
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        this.LogMeasures(EventNameResponsive, fields);
    }

    public void LogProcessedArgumentType(string type)
    {
        var fields = new LoggingFields();
        fields.AddString("Type", type);
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        this.LogMeasures(EventNameActivationArgumentsProcessed, fields);
    }

    public void LogStateChanged(string newState)
    {
        var fields = new LoggingFields();
        fields.AddString("State", newState);
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        LogMeasures(EventNameStateChanged, fields);
    }

    public void LogUpgradeError(Exception e)
    {
        var fields = new LoggingFields();
        fields.AddInt32("HResult", e.HResult);
        fields.AddString("Message", e.Message);
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        this.LogMeasures(EventNameUpgradeError, fields);
    }

    public void LogAcquiringPackages()
    {
        var fields = new LoggingFields();
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        this.LogMeasures(EventNameAcquiringPackages, fields);
    }

    public void LogStartingSilentUpdate()
    {
        var fields = new LoggingFields();
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        this.LogMeasures(EventNameStartSilentUpdate, fields);
    }

    public void LogStartingInteractiveUpdate()
    {
        var fields = new LoggingFields();
        fields.AddUInt64(PDTPrivacyDataTag, PDTProductAandServiceUsage);
        this.LogMeasures(EventNameStartInteractiveUpdate, fields);
    }
}
