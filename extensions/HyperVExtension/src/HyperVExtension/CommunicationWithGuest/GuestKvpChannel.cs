// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management;
using System.Xml;
using System.Xml.XPath;
using HyperVExtension.HostGuestCommunication;
using Serilog;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle communication with guest VM using Hyper-V KVP service.
/// </summary>
internal sealed class GuestKvpChannel : IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GuestKvpChannel));

    // Public documentation doesn't say that there is a limit on the size of the value
    // smaller than registry key values. But in the sample code for linux integration services
    // HV_KVP_EXCHANGE_MAX_KEY_SIZE is used as a limit. In Windows code it's defined as 2048 (bytes).
    // We'll need to split the message into smaller parts if it's too long.
    private const int MaxValueCount = 1000;
    private readonly Guid _vmId;
    private readonly ManagementObject _virtualSystemService;
    private readonly ManagementScope _scope;
    private readonly ManagementObject _vmWmi;
    private readonly List<string> _kvpNamesToCleanup = [];
    private bool _disposed;

    public GuestKvpChannel(Guid vmId)
    {
        _vmId = vmId;
        _scope = new ManagementScope(@"root\virtualization\v2", null);
        _virtualSystemService = WmiUtility.GetServiceObject(_scope, "Msvm_VirtualSystemManagementService");
        _vmWmi = WmiUtility.GetTargetComputer(_vmId, _scope);
    }

    public void SendMessage(IRequestMessage requestMessage, uint communicationIdCounter, CancellationToken stoppingToken)
    {
        // Check if message is too large and split into multiple parts.
        var numberOfParts = requestMessage.RequestData.Length / MaxValueCount;
        if (requestMessage.RequestData.Length % MaxValueCount != 0)
        {
            numberOfParts++;
        }

        var kvpNameStart = $"{MessageHelper.DevSetupPrefix}{{{communicationIdCounter}}}{MessageHelper.Separator}";
        var kvpNameEnd = $"{MessageHelper.Separator}{numberOfParts}";

        // Try to remove all parts of the message first just in case if previous communication session was
        // abruptly terminates and we got old messages not removed.
        for (var i = 0; i < numberOfParts; i++)
        {
            RemoveKvpItem($"{kvpNameStart}{i}{kvpNameEnd}");
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        var index = 0;
        foreach (var subString in requestMessage.RequestData.SplitByLength(MaxValueCount))
        {
            index++;
            var kvpName = $"{kvpNameStart}{index}{kvpNameEnd}";
            lock (_kvpNamesToCleanup)
            {
                _kvpNamesToCleanup.Add(kvpName);
            }

            AddKvpItem(kvpName, subString);
        }
    }

    private void AddKvpItem(string name, string value)
    {
        using ManagementClass kvpExchangeDataItem = new ManagementClass(_scope, new ManagementPath("Msvm_KvpExchangeDataItem"), null);
        using ManagementObject dataItem = kvpExchangeDataItem.CreateInstance();
        dataItem["Data"] = value;
        dataItem["Name"] = name;
        dataItem["Source"] = 0;

        var dataItems = new string[1];
        dataItems[0] = dataItem.GetText(TextFormat.CimDtd20);

        using ManagementBaseObject inParams = _virtualSystemService.GetMethodParameters("AddKvpItems");
        inParams["TargetSystem"] = _vmWmi.Path.Path;
        inParams["DataItems"] = dataItems;

        using ManagementBaseObject outParams = _virtualSystemService.InvokeMethod("AddKvpItems", inParams, null);
        if ((uint)outParams["ReturnValue"] == (uint)WmiUtility.ReturnCode.Started)
        {
            uint errorCode;
            string errorDescription;
            if (!WmiUtility.JobCompleted(outParams, _scope, out errorCode, out errorDescription))
            {
                throw new System.ComponentModel.Win32Exception((int)errorCode, $"Cannot send message to VM '{_vmId.ToString("D")}': '{errorDescription}'.");
            }
        }
        else if ((uint)outParams["ReturnValue"] != (uint)WmiUtility.ReturnCode.Completed)
        {
            throw new System.ComponentModel.Win32Exception((int)outParams["ReturnValue"], $"Cannot send message to VM '{_vmId.ToString("D")}': '{outParams["ReturnValue"]}'.");
        }
        else
        {
            _log.Information($"Sent message to '{_vmId.ToString("D")}' VM. Message ID: '{name}'.");
        }
    }

    /// <summary>
    /// Delete Hyper-V KVP message from the VM.
    /// Best effort. Log error if failed, but don't throw.
    /// </summary>
    /// <param name="name">Hyper-V KVP name.</param>
    private void RemoveKvpItem(string name)
    {
        try
        {
            using ManagementClass kvpExchangeDataItem = new ManagementClass(_scope, new ManagementPath("Msvm_KvpExchangeDataItem"), null);
            using ManagementObject dataItem = kvpExchangeDataItem.CreateInstance();
            dataItem["Data"] = string.Empty;
            dataItem["Name"] = name;
            dataItem["Source"] = 0;

            var dataItems = new string[1];
            dataItems[0] = dataItem.GetText(TextFormat.CimDtd20);

            using ManagementBaseObject inParams = _virtualSystemService.GetMethodParameters("RemoveKvpItems");
            inParams["TargetSystem"] = _vmWmi.Path.Path;
            inParams["DataItems"] = dataItems;

            using ManagementBaseObject outParams = _virtualSystemService.InvokeMethod("RemoveKvpItems", inParams, null);
            if ((uint)outParams["ReturnValue"] == (uint)WmiUtility.ReturnCode.Started)
            {
                uint errorCode;
                string errorDescription;
                if (!WmiUtility.JobCompleted(outParams, _scope, out errorCode, out errorDescription))
                {
                    throw new System.ComponentModel.Win32Exception((int)errorCode, $"Cannot delete message from VM '{_vmId.ToString("D")}': '{errorDescription}'.");
                }
            }
            else if ((uint)outParams["ReturnValue"] != (uint)WmiUtility.ReturnCode.Completed)
            {
                throw new System.ComponentModel.Win32Exception((int)outParams["ReturnValue"], $"Cannot delete message from VM '{_vmId.ToString("D")}': '{outParams["ReturnValue"]}'.");
            }
            else
            {
                _log.Information($"Deleted message from '{_vmId.ToString("D")}' VM. Message ID: '{name}'.");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to delete message from '{_vmId.ToString("D")}' VM. Message ID: '{name}'.");
        }
    }

    public List<IResponseMessage> WaitForResponseMessages(string communicationId, TimeSpan timeout, bool expectProgressResponse, CancellationToken stoppingToken)
    {
        var waitTime = TimeSpan.FromMilliseconds(500);
        var waitTimeLeft = timeout;
        while ((waitTimeLeft > TimeSpan.Zero) && !stoppingToken.IsCancellationRequested)
        {
            var messages = TryReadResponseMessages(communicationId, expectProgressResponse, stoppingToken);
            if (messages.Count > 0)
            {
                return messages;
            }

            stoppingToken.WaitHandle.WaitOne(waitTime);
            waitTimeLeft -= waitTime;
        }

        return new List<IResponseMessage>();
    }

    private List<IResponseMessage> TryReadResponseMessages(string communicationId, bool expectProgressResponse, CancellationToken stoppingToken)
    {
        var guestKvps = ReadGuestKvps();
        var result = new List<IResponseMessage>();
        guestKvps.TryGetValue(communicationId, out var responseData);

        IResponseMessage? progressResponse = null;
        if (responseData != null)
        {
            progressResponse = new ResponseMessage(communicationId, responseData);
        }

        if (!expectProgressResponse)
        {
            if (progressResponse != null)
            {
                result.Add(progressResponse);
            }
        }
        else
        {
            // Find all progress message in "<responseId>_Progress_<sequence number>"
            // Then sort them by sequence number and add to the result list.
            var progressResponseId = $"{communicationId}_Progress_";
            var kvps = guestKvps.Where(kvp => kvp.Key.StartsWith(progressResponseId, StringComparison.OrdinalIgnoreCase));
            var orderedResponses = new Dictionary<uint, KeyValuePair<string, string>>();
            foreach (var kvp in kvps)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return result;
                }

                var progressIdParts = kvp.Key.Split('_');
                if (uint.TryParse(progressIdParts[2], out var order))
                {
                    orderedResponses.Add(order, kvp);
                }
            }

            foreach (var kvp in orderedResponses.OrderBy(kvp => kvp.Key))
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return result;
                }

                result.Add(new ResponseMessage(kvp.Value.Key, kvp.Value.Value));
            }

            // Progress messages first then the final response message.
            if (progressResponse != null)
            {
                result.Add(progressResponse);
            }
        }

        return result;
    }

    private Dictionary<string, string> ReadGuestKvps()
    {
        return MessageHelper.MergeMessageParts(ReadRawGuestKvps());
    }

    private Dictionary<string, string> ReadRawGuestKvps()
    {
        Dictionary<string, string> guestKvps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var collection = _vmWmi.GetRelated("Msvm_KvpExchangeComponent");
        foreach (ManagementObject kvpExchangeComponent in collection)
        {
            foreach (var exchangeDataItem in (string[])kvpExchangeComponent["GuestExchangeItems"])
            {
                XPathDocument xpathDoc = new XPathDocument(XmlReader.Create(new StringReader(exchangeDataItem)));
                XPathNavigator navigator = xpathDoc.CreateNavigator();
                XPathNavigator? navigatorName = navigator.SelectSingleNode("/INSTANCE/PROPERTY[@NAME='Name']/VALUE/child::text()");

                if (navigatorName != null)
                {
                    XPathNavigator? navigatorData = navigator.SelectSingleNode("/INSTANCE/PROPERTY[@NAME='Data']/VALUE/child::text()");
                    if (navigatorData != null)
                    {
                        var name = navigatorName.Value.ToString();
                        var value = navigatorData.Value.ToString();
                        guestKvps[name] = value;
                    }
                }
            }
        }

        return guestKvps;
    }

    public void CleanUp()
    {
        lock (_kvpNamesToCleanup)
        {
            foreach (var kvpName in _kvpNamesToCleanup)
            {
                RemoveKvpItem(kvpName);
            }

            _kvpNamesToCleanup.Clear();
        }
    }

    public void CleanUp(string communicationId)
    {
        lock (_kvpNamesToCleanup)
        {
            List<string> kvpNamesToDelete = [];
            foreach (var kvpName in _kvpNamesToCleanup)
            {
                if (kvpName.StartsWith(communicationId, StringComparison.OrdinalIgnoreCase))
                {
                    kvpNamesToDelete.Add(kvpName);
                    RemoveKvpItem(kvpName);
                }
            }

            foreach (var kvpName in kvpNamesToDelete)
            {
                _kvpNamesToCleanup.Remove(kvpName);
            }
        }
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
                CleanUp();
                _vmWmi?.Dispose();
                _virtualSystemService?.Dispose();
            }

            _disposed = true;
        }
    }
}
