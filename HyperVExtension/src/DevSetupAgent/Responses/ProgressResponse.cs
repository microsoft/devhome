// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.HostGuestCommunication;
using Microsoft.Windows.DevHome.DevSetupEngine;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class that generates JSON data for Configure command progress events..
/// JSON payload is generated in GenerateJsonData virtual method.
/// {
///   "ResponseId": "DevSetup{10000000-1000-1000-1000-100000000000}_Progres_<counter>",
///   "ResponseType": "Progress",
///   "Timestamp":"2023-11-21T08:08:58.6287789Z",
///   "Version": "0.0.1",
///   <request specific data>
/// }
/// </summary>
internal sealed class ProgressResponse : ResponseBase
{
    private readonly ConfigurationSetChangeData _progressData;

    private uint ProgressCounter { get; }

    public ProgressResponse(string requestId, IConfigurationSetChangeData progressData, uint progressCounter)
        : base(requestId, ConfigureRequest.RequestTypeId)
    {
        _progressData = new ConfigurationSetChangeData().Populate(progressData);
        ProgressCounter = progressCounter;
        ResponseId = RequestId + $"_Progress_{ProgressCounter}";
        ResponseType = "Progress";
        GenerateJsonData();
    }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();
        var progress = JsonSerializer.Serialize(_progressData);
        JsonData![nameof(ProgressCounter)] = ProgressCounter;
        JsonData![nameof(ConfigurationSetChangeData)] = progress;
    }
}
