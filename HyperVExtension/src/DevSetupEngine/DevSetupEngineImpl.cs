// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Windows.DevHome.DevSetupEngine;
using Windows.Foundation;

namespace HyperVExtension.DevSetupEngine;

/// <summary>
/// Implementation of the COM interface IDevSetupEngine.
/// </summary>
[ComVisible(true)]
[Guid("82E86C64-A8B9-44F9-9323-C37982F2D8BE")]
[ComDefaultInterface(typeof(IDevSetupEngine))]
internal sealed class DevSetupEngineImpl : IDevSetupEngine, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the synchronization object that is used to prevent the main program from exiting
    /// until the object is disposed.
    /// </summary>
    public ManualResetEvent ComServerDisposedEvent { get; } = new(false);

    public IAsyncOperationWithProgress<IApplyConfigurationResult, IConfigurationSetChangeData> ApplyConfigurationAsync(string content)
    {
        var configurationFileHelper = new ConfigurationFileHelper();
        return AsyncInfo.Run<IApplyConfigurationResult, IConfigurationSetChangeData>(async (cancellationToken, progress) =>
        {
            return await configurationFileHelper.ApplyConfigurationAsync(content, progress);
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ComServerDisposedEvent.Set();
            }

            _disposed = true;
        }
    }
}
