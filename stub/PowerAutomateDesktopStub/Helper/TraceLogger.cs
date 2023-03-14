// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using Windows.Foundation.Diagnostics;

namespace Microsoft.PowerAutomateDesktop.Stub.Helper
{
	internal class TraceLogger
	{
		private const string EVENT_NAME_RESPONSIVE = "InstallControlResponsive";
		private const string EVENT_NAME_STATE_CHANGED = "StateChanged";
		private const string EVENT_NAME_ACTIVATION_ARGUMENTS_PROCESSED = "ActivationArgumentsProcessed";
		private const string EVENT_NAME_UPGRADE_ERROR = "UpgradeError";
		private const string EVENT_NAME_ACQUIRING_PACKAGES = "AcquiringPackages";
		private const string EVENT_NAME_START_SILENT_UPDATE = "StartingSilentUpdate";
		private const string EVENT_NAME_START_INTERACTIVE_UPDATE = "StartingInteractiveUpdate";

		private const string PDT_PRIVACY_DATA_TAG = "PartA_PrivTags";
		private const UInt32 PDT_PRODUCT_AND_SERVICE_USAGE = 0x0000_0000_0200_0000u;

		// c.f. WINEVENT_KEYWORD_RESERVED_63-56 0xFF00000000000000 // Bits 63-56 - channel keywords
		// c.f. WINEVENT_KEYWORD_*              0x00FF000000000000 // Bits 55-48 - system-reserved keywords
		private const Int64 MICROSOFT_KEYWORD_CRITICAL = 0x0000800000000000; // Bit 47
		private const Int64 MICROSOFT_KEYWORD_MEASURES = 0x0000400000000000; // Bit 46
		private const Int64 MICROSOFT_KEYWORD_TELEMETRY = 0x0000200000000000; // Bit 45

		private LoggingChannel stubProvider;
		
		public static TraceLogger Instance { get; } = new TraceLogger();

		public TraceLogger()
		{
			// TODO: this is a straight copy from Microsoft.Apps.Stubs, maybe it should be unique
			this.stubProvider = new LoggingChannel("Microsoft.Apps.Stubs", new LoggingChannelOptions(new Guid(0x4f50731a, 0x89cf, 0x4782, 0xb3, 0xe0, 0xdc, 0xe8, 0xc9, 0x4, 0x76, 0xba)));
		}

		public void LogCritical(string eventName, LoggingFields fields)
		{
			this.stubProvider.LogEvent(eventName, fields, LoggingLevel.Verbose, new LoggingOptions(MICROSOFT_KEYWORD_CRITICAL));
		}

		public void LogMeasures(string eventName, LoggingFields fields)
		{
			this.stubProvider.LogEvent(eventName, fields, LoggingLevel.Verbose, new LoggingOptions(MICROSOFT_KEYWORD_MEASURES));
		}

		public void LogTelemetry(string eventName, LoggingFields fields)
		{
			this.stubProvider.LogEvent(eventName, fields, LoggingLevel.Verbose, new LoggingOptions(MICROSOFT_KEYWORD_TELEMETRY));
		}

		public void LogResponsive()
		{
			var fields = new LoggingFields();
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			this.LogMeasures(EVENT_NAME_RESPONSIVE, fields);
		}

		public void LogProcessedArgumentType(string type)
		{
			var fields = new LoggingFields();
			fields.AddString("Type", type);
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			this.LogMeasures(EVENT_NAME_ACTIVATION_ARGUMENTS_PROCESSED, fields);
		}

		public void LogStateChanged(string newState)
		{
			var fields = new LoggingFields();
			fields.AddString("State", newState);
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			LogMeasures(EVENT_NAME_STATE_CHANGED, fields);
		}

		public void LogUpgradeError(Exception e)
		{
			var fields = new LoggingFields();
			fields.AddInt32("HResult", e.HResult);
			fields.AddString("Message", e.Message);
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			this.LogMeasures(EVENT_NAME_UPGRADE_ERROR, fields);
		}

		public void LogAcquiringPackages()
		{
			var fields = new LoggingFields();
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			this.LogMeasures(EVENT_NAME_ACQUIRING_PACKAGES, fields);
		}

		public void LogStartingSilentUpdate()
		{
			var fields = new LoggingFields();
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			this.LogMeasures(EVENT_NAME_START_SILENT_UPDATE, fields);
		}

		public void LogStartingInteractiveUpdate()
		{
			var fields = new LoggingFields();
			fields.AddUInt64(PDT_PRIVACY_DATA_TAG, PDT_PRODUCT_AND_SERVICE_USAGE);
			this.LogMeasures(EVENT_NAME_START_INTERACTIVE_UPDATE, fields);
		}
	}
}
