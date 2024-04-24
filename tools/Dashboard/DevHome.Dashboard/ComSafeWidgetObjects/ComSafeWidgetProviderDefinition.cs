// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Storage.Streams;

namespace DevHome.Dashboard.ComSafeWidgetObjects;

/// <summary>
/// Since WidgetProviderDefinitions are OOP COM objects, we need to wrap them in a safe way to handle COM exceptions
/// that arise when the underlying OOP object vanishes. All WidgetProviderDefinitions should be wrapped in a
/// ComSafeWidgetProviderDefinition and calls to the WidgetProviderDefinition should be done through the ComSafeWidgetProviderDefinition.
/// This class will handle the COM exceptions and get a new OOP WidgetProviderDefinition if needed.
/// All APIs on the IWidgetProviderDefinition interface is reflected here.
/// </summary>
public class ComSafeWidgetProviderDefinition : IDisposable
{
    private WidgetProviderDefinition _oopWidgetProviderDefinition;

    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComSafeWidgetProviderDefinition));

    private readonly SemaphoreSlim _getDefinitionLock = new(1, 1);
    private bool _disposedValue;

    private bool _hasValidProperties;
    private const int MaxAttempts = 3;

    public string DisplayName { get; private set; }

    // Not currently used.
    public IRandomAccessStreamReference Icon => throw new NotImplementedException();

    public string Id { get; private set; }

    public ComSafeWidgetProviderDefinition(string widgetProviderDefinitionId)
    {
        Id = widgetProviderDefinitionId;
    }

    public ComSafeWidgetProviderDefinition(WidgetProviderDefinition widgetProviderDefinition)
    {
        _oopWidgetProviderDefinition = widgetProviderDefinition;
    }

    public async Task<bool> Populate()
    {
        await LoadOopWidgetProviderDefinitionAsync();
        return _hasValidProperties;
    }

    private async Task LoadOopWidgetProviderDefinitionAsync()
    {
        var attempt = 0;
        await _getDefinitionLock.WaitAsync();
        try
        {
            while (attempt++ < 3 && (_oopWidgetProviderDefinition == null || _hasValidProperties == false))
            {
                try
                {
                    _oopWidgetProviderDefinition ??= await Application.Current.GetService<IWidgetHostingService>().GetProviderDefinitionAsync(Id);

                    if (!_hasValidProperties)
                    {
                        await Task.Run(() =>
                        {
                            DisplayName = _oopWidgetProviderDefinition.DisplayName;
                            Id = _oopWidgetProviderDefinition.Id;
                            _hasValidProperties = true;
                        });
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to get properties of out-of-proc object");
                }
            }
        }
        finally
        {
            _getDefinitionLock.Release();
        }
    }

    /// <summary>
    /// Get a WidgetProviderDefinition's ID from a WidgetProviderDefinition object.
    /// </summary>
    /// <param name="widgetProviderDefinition">WidgetProviderDefinition</param>
    /// <returns>The WidgetProviderDefinition's Id, or in the case of failure string.Empty</returns>
    public static async Task<string> GetIdFromUnsafeWidgetProviderDefinitionAsync(WidgetProviderDefinition widgetProviderDefinition)
    {
        return await Task.Run(() =>
        {
            try
            {
                return widgetProviderDefinition.Id;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
            }

            return string.Empty;
        });
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _getDefinitionLock.Dispose();
            }

            _disposedValue = true;
        }
    }
}
