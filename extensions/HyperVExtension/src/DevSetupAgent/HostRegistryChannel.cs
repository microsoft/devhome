// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Text;
using DevHome.Common;
using HyperVExtension.HostGuestCommunication;
using Microsoft.Win32;
using Serilog;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Implementation of IHostChannel using registry keys provided by Hyper-V Data Exchange Service (KVP).
/// https://learn.microsoft.com/virtualization/hyper-v-on-windows/reference/integration-services#hyper-v-data-exchange-service-kvp
/// https://learn.microsoft.com/previous-versions/windows/it-pro/windows-server-2012-R2-and-2012/dn798287(v=ws.11)
/// "HKLM\SOFTWARE\Microsoft\Virtual Machine\External" contains data pushed to the guest from the host by a user
/// "HKLM\SOFTWARE\Microsoft\Virtual Machine\Guest" contains data created on the guest. This data is available to the host as non-intrinsic data.
/// Host client will create registry value named "DevSetup{<number>}~<index>~<total>" with JSON message as a string value.
/// The name of the registry value becomes "MessageId" and will be used for response so the client can match
/// request with response.
/// </summary>
public sealed class HostRegistryChannel : IHostChannel, IDisposable
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(HostRegistryChannel));

    // Public documentation doesn't say that there is a limit on the size of the value
    // smaller than registry key values. But in the sample code for linux integration services
    // HV_KVP_EXCHANGE_MAX_KEY_SIZE is used as a limit. In Windows code it's defined as 2048 (bytes).
    // We'll need to split the message into smaller parts if it's too long.
    private const int MaxValueCount = 1000;
    private readonly string _fromHostRegistryKeyPath;
    private readonly string _toHostRegistryKeyPath;
    private readonly RegistryWatcher _registryWatcher;
    private readonly AutoResetEvent _registryKeyChangedEvent;
    private readonly RegistryKey _registryHiveKey;
    private bool _disposed;

    public HostRegistryChannel(IRegistryChannelSettings registryChannelSettings)
    {
        _fromHostRegistryKeyPath = registryChannelSettings.FromHostRegistryKeyPath;
        _toHostRegistryKeyPath = registryChannelSettings.ToHostRegistryKeyPath;

        // If running x86 version on x64 OS, we need to open 64-bit registry view.
        _registryHiveKey = RegistryKey.OpenBaseKey(registryChannelSettings.RegistryHive, RegistryView.Registry64);

        // Search and delete all existing registry values with name "DevSetup{<number>}"
        MessageHelper.DeleteAllMessages(_registryHiveKey, _toHostRegistryKeyPath, MessageHelper.MessageIdStart);
        MessageHelper.DeleteAllMessages(_registryHiveKey, _fromHostRegistryKeyPath, MessageHelper.MessageIdStart);

        _registryKeyChangedEvent = new AutoResetEvent(true);
        _registryWatcher = new RegistryWatcher(_registryHiveKey, _fromHostRegistryKeyPath, OnDevSetupKeyChanged);
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
                if (!string.IsNullOrEmpty(requestMessage.CommunicationId))
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
                    var regKey = _registryHiveKey.CreateSubKey(_toHostRegistryKeyPath);
                    if (regKey == null)
                    {
                        _log.Error($"Cannot open {_toHostRegistryKeyPath} registry key. Error: {Marshal.GetLastWin32Error()}");
                        return;
                    }

                    // Split message into parts due to the Hyper-V KVP service 2048 byte registry value limit.
                    var numberOfParts = responseMessage.ResponseData.Length / MaxValueCount;
                    if (responseMessage.ResponseData.Length % MaxValueCount != 0)
                    {
                        numberOfParts++;
                    }

                    var totalStr = $"{MessageHelper.Separator}{numberOfParts}";
                    var index = 0;
                    foreach (var subString in responseMessage.ResponseData.SplitByLength(MaxValueCount))
                    {
                        index++;
                        regKey.SetValue($"{responseMessage.CommunicationId}{MessageHelper.Separator}{index}{totalStr}", subString, RegistryValueKind.String);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Could not write host message. Response ID: {responseMessage.CommunicationId}");
                }
            },
            stoppingToken);
    }

    public async void DeleteResponseMessageAsync(string communicationId, CancellationToken stoppingToken)
    {
        await Task.Run(
            () =>
            {
                try
                {
                    MessageHelper.DeleteAllMessages(_registryHiveKey, _toHostRegistryKeyPath, communicationId);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Could not delete host message. Response ID: {communicationId}");
                }
            },
            stoppingToken);
    }

    private RequestMessage TryReadMessage()
    {
        var requestMessage = default(RequestMessage);
        try
        {
            // Messages are split in parts to workaround HyperV KVP service the 2048 bytes limit of registry value.
            // We need to merge all parts of the message before processing it.
            // TODO: Modify this class to use MessageHelper.MergeMessageParts (requires changing return value and handling in the caller).
            HashSet<string> ignoreMessages = new();
            var regKey = _registryHiveKey.OpenSubKey(_fromHostRegistryKeyPath, true);
            var valueNames = regKey?.GetValueNames();
            if (valueNames != null)
            {
                foreach (var valueName in valueNames)
                {
                    if (valueName.StartsWith(MessageHelper.MessageIdStart, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var s = valueName.Split(MessageHelper.Separator);
                        if (!MessageHelper.IsValidMessageName(s, out var index, out var total))
                        {
                            continue;
                        }

                        if (ignoreMessages.Contains(s[0]))
                        {
                            continue;
                        }

                        // Count if we have all parts of the message
                        var count = 0;
                        foreach (var valueNameTmp in valueNames)
                        {
                            if (valueNameTmp.StartsWith($"{s[0]}{MessageHelper.Separator}", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!MessageHelper.IsValidMessageName(valueNameTmp.Split(MessageHelper.Separator), out var indeTmp, out var totalTmp))
                                {
                                    continue;
                                }

                                count++;
                            }
                        }

                        if (count != total)
                        {
                            // Ignore this message for now. We don't have all parts.
                            ignoreMessages.Add(s[0]);
                            continue;
                        }

                        // Merge all parts of the message
                        // Preserve communication id ("DevSetup{<number>}"), delete the value and create response even if reading failed.
                        requestMessage.CommunicationId = s[0];
                        try
                        {
                            var sb = new StringBuilder();
                            for (var i = 1; i <= total; i++)
                            {
                                var value1 = (string?)regKey!.GetValue($"{requestMessage.CommunicationId}{MessageHelper.Separator}{i}{MessageHelper.Separator}{total}");
                                if (value1 == null)
                                {
                                    throw new InvalidOperationException($"Could not read guest message {valueName}");
                                }

                                sb.Append(value1);
                            }

                            requestMessage.RequestData = sb.ToString();
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Could not read host message {valueName}");
                        }

                        MessageHelper.DeleteAllMessages(_registryHiveKey, _fromHostRegistryKeyPath, requestMessage.CommunicationId);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Could not read host message.");
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
}
