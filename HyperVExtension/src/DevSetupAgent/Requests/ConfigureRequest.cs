// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Microsoft.Windows.DevHome.DevSetupEngine;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// WinGet Configure request from the client.
/// JSON payload is converted to request properties.
/// {
///   "RequestId": "DevSetup{10000000-1000-1000-1000-100000000000}",
///   "RequestType": "GetVersion",
///   "Timestamp":"2023-11-21T08:08:58.6287789Z"
///   "Configure":<WinGet configure yaml>
/// }
/// </summary>
internal sealed class ConfigureRequest : RequestBase
{
    public ConfigureRequest(IRequestContext requestContext)
        : base(requestContext)
    {
        ConfigureData = GetRequiredStringValue("Configure").Replace("\\n", System.Environment.NewLine);
    }

    public string ConfigureData { get; }

    public override IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        // DevSetupEngine needs to be started manually from command line in the test.
        var devSetupEnginePtr = IntPtr.Zero;
        try
        {
            var hr = PInvoke.CoCreateInstance(Guid.Parse("82E86C64-A8B9-44F9-9323-C37982F2D8BE"), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IDevSetupEngine).GUID, out var devSetupEngineObj);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            devSetupEnginePtr = Marshal.GetIUnknownForObject(devSetupEngineObj);

            var devSetupEngine = MarshalInterface<IDevSetupEngine>.FromAbi(devSetupEnginePtr);
            var operation = devSetupEngine.ApplyConfigurationAsync(ConfigureData);

            uint progressCounter = 0;
            operation.Progress = (operation, data) =>
            {
                System.Diagnostics.Trace.WriteLine($"  - Unit: {data.Unit.Type} [{data.UnitState}]");
                var progressResponse = new ProgressResponse(RequestMessage.RequestId!, data, ++progressCounter);
                progressHandler(progressResponse, stoppingToken);
            };

            operation.AsTask().Wait(stoppingToken);
            var result = operation.GetResults();

            return new ConfigureResponse(RequestMessage.RequestId!, result);
        }
        finally
        {
            if (devSetupEnginePtr != IntPtr.Zero)
            {
                Marshal.Release(devSetupEnginePtr);
            }
        }
    }
}
