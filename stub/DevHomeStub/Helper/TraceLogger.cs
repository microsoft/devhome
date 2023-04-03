// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using Windows.Foundation.Diagnostics;

namespace Microsoft.DevHome.Stub.Helper
{
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

        private readonly LoggingChannel stubProvider;

        public static TraceLogger Instance { get; } = new TraceLogger();

        public TraceLogger()
        {
            // TODO: this is a straight copy from Microsoft.Apps.Stubs, maybe it should be unique
            this.stubProvider = new LoggingChannel("Microsoft.Apps.Stubs", new LoggingChannelOptions(new Guid(0x4f50731a, 0x89cf, 0x4782, 0xb3, 0xe0, 0xdc, 0xe8, 0xc9, 0x4, 0x76, 0xba)));
        }

        public void LogCritical(string eventName, LoggingFields fields)
        {
            this.stubProvider.LogEvent(eventName, fields, LoggingLevel.Verbose, new LoggingOptions(MicrosoftKeywordCritical));
        }

        public void LogMeasures(string eventName, LoggingFields fields)
        {
            this.stubProvider.LogEvent(eventName, fields, LoggingLevel.Verbose, new LoggingOptions(MicrosoftKeywordMeasures));
        }

        public void LogTelemetry(string eventName, LoggingFields fields)
        {
            this.stubProvider.LogEvent(eventName, fields, LoggingLevel.Verbose, new LoggingOptions(MicrosoftKeywordTelemetry));
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
}
