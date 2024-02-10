// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.Logging;
using Microsoft.Win32;
using Windows.Foundation.Collections;

namespace HyperVExtension.DevSetupAgent;

// TODO: figure out when to delete sent messages from the registry.
// Host cannot delete them (no such functionality in the Hyper-V KVP service).
// We can delete them after some period of time.

/// <summary>
/// Implementation of IHostChannel using registry keys provided by Hyper-V Data Exchange Service (KVP).
/// https://learn.microsoft.com/virtualization/hyper-v-on-windows/reference/integration-services#hyper-v-data-exchange-service-kvp
/// https://learn.microsoft.com/previous-versions/windows/it-pro/windows-server-2012-R2-and-2012/dn798287(v=ws.11)
/// "HKLM\SOFTWARE\Microsoft\Virtual Machine\External" contains data pushed to the guest from the host by a user
/// "HKLM\SOFTWARE\Microsoft\Virtual Machine\Guest" contains data created on the guest. This data is available to the host as non-intrinsic data.
/// Host client will create registry value named "DevSetup{<GUID>}" with JSON message as a string value.
/// The name of the registry value becomes "MessageId" and will be used for response so the client can match
/// request with response.
/// </summary>
public sealed class HostRegistryChannel : IHostChannel, IDisposable
{
    private readonly string _fromHostRegistryKeyPath;
    private readonly string _toHostRegistryKeyPath;
    private readonly RegistryWatcher _registryWatcher;
    private readonly AutoResetEvent _registryKeyChangedEvent;
    private readonly RegistryKey _registryHive = Registry.CurrentUser;
    private bool _disposed;

    public HostRegistryChannel(IRegistryChannelSettings registryChannelSettings)
    {
        _fromHostRegistryKeyPath = registryChannelSettings.FromHostRegistryKeyPath;
        _toHostRegistryKeyPath = registryChannelSettings.ToHostRegistryKeyPath;
        _registryHive = registryChannelSettings.RegistryHive;

        // Search and delete all existing registry values with name "DevSetup{<GUID>}"
        DeleteAllMessages(_registryHive, _toHostRegistryKeyPath);
        DeleteAllMessages(_registryHive, _fromHostRegistryKeyPath);

        _registryKeyChangedEvent = new AutoResetEvent(true);
        _registryWatcher = new RegistryWatcher(_registryHive, _fromHostRegistryKeyPath, OnDevSetupKeyChanged);
    }

    private void OnDevSetupKeyChanged()
    {
        _registryKeyChangedEvent.Set();
    }

    public async Task<IRequestMessage> WaitForMessageAsync(CancellationToken stoppingToken)
    {
        try
        {
            var requestMessage = default(RequestMessage);
            _registryWatcher.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                requestMessage = TryReadMessage();
                if (!string.IsNullOrEmpty(requestMessage.RequestId))
                {
                    break;
                }

                await Task.Run(() => WaitHandle.WaitAny(new[] { _registryKeyChangedEvent, stoppingToken.WaitHandle }));
            }

            return requestMessage;
        }
        finally
        {
            _registryWatcher.Stop();
        }
    }

    public async void SendMessageAsync(IResponseMessage responseMessage, CancellationToken stoppingToken)
    {
        await Task.Run(
            () =>
        {
            try
            {
                var regKey = _registryHive.CreateSubKey(_toHostRegistryKeyPath);
                if (regKey == null)
                {
                    Logging.Logger()?.ReportError($"Cannot open {_toHostRegistryKeyPath} registry key. Error: {Marshal.GetLastWin32Error()}");
                    return;
                }

                regKey.SetValue(responseMessage.ResponseId, responseMessage.ResponseData, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError($"Could not write host message. Response ID: {responseMessage.ResponseId}", ex);
            }
        },
            stoppingToken);
    }

    private RequestMessage TryReadMessage()
    {
        var requestMessage = default(RequestMessage);
        try
        {
            var regKey = _registryHive.OpenSubKey(_fromHostRegistryKeyPath, true);
            var values = regKey?.GetValueNames();
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value.StartsWith("DevSetup{", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Preserve message GUID, delete the value and create response even if reading failed.
                        requestMessage.RequestId = value;
                        try
                        {
                            requestMessage.RequestData = (string?)regKey!.GetValue(value);
                        }
                        catch (Exception ex)
                        {
                            Logging.Logger()?.ReportError($"Could not read host message {value}", ex);
                        }

                        regKey!.DeleteValue(value, false);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Logger()?.ReportError("Could not read host message.", ex);
        }

        return requestMessage;
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
                _registryKeyChangedEvent.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Search and delete all existing registry values with name "DevSetup{*"
    /// </summary>
    /// <param name="registryKey">Parent registry key.</param>
    /// <param name="registryKeyPath">Registry key sub-path to search.</param>
    private void DeleteAllMessages(RegistryKey registryKey, string registryKeyPath)
    {
        var regKey = registryKey.OpenSubKey(registryKeyPath, true);
        var values = regKey?.GetValueNames();
        if (values != null)
        {
            foreach (var value in values)
            {
                if (value.StartsWith("DevSetup{", StringComparison.InvariantCultureIgnoreCase))
                {
                    regKey!.DeleteValue(value, false);
                }
            }
        }
    }
}
