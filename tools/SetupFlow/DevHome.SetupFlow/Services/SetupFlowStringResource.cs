// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Services;
using Microsoft.Extensions.Options;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;

namespace DevHome.SetupFlow.Services;

public class SetupFlowStringResource : StringResource, ISetupFlowStringResource
{
    public SetupFlowStringResource(IOptions<SetupFlowOptions> setupFlowOptions)
        : base(setupFlowOptions.Value.StringResourceName, setupFlowOptions.Value.StringResourcePath)
    {
    }

    public string GetLocalizedErrorMsg(int errorCode, string logComponent)
    {
        unsafe
        {
            PWSTR formattedMessage;
            var msgLength = PInvoke.FormatMessage(
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_SYSTEM |
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS,
                null,
                unchecked((uint)errorCode),
                0,
                (PWSTR)(void*)&formattedMessage,
                0,
                null);
            try
            {
                if (msgLength == 0)
                {
                    // if formatting the error code into a message fails, then log this and just return the error code.
                    var log = Log.ForContext("SourceContext", nameof(SetupFlowStringResource));
                    log.Error($"Failed to format error code.  0x{errorCode:X}");
                    return $"(0x{errorCode:X})";
                }

                return new string(formattedMessage.Value, 0, (int)msgLength) + $" (0x{errorCode:X})";
            }
            finally
            {
                PInvoke.LocalFree((HLOCAL)(IntPtr)formattedMessage.Value);
            }
        }
    }
}
