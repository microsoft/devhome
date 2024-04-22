// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.HostGuestCommunication;
using Microsoft.Windows.DevHome.DevSetupEngine;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class to generate JSON data for Configure command responses.
///
/// {
///  "ResponseId": "DevSetup{10000000-1000-1000-1000-100000000000}",
///  "ResponseType": "Configure",
///  "Timestamp":"2023-11-21T08:08:58.6287789Z",
///  "ApplyConfigurationResult":
///  {
///       "OpenConfigurationSetResult":
///       {
///           "ResultCode":"0",
///           "Field":"MyField",
///           "Value":"MyValue",
///           "Line":"2",
///           "Column":"5"
///       },
///       "ApplyConfigurationSetResult"
///       {
///           "ResultCode":"0",
///           "UnitResults":
///           [
///             {
///               "Unit":
///               {
///                   "UnitName":"OsVersion",
///                   "Identifier":"10000000-1000-1000-1000-100005550033",
///                   "State":"Completed",
///                   "IsGroup":"false",
///                   "Units":[]
///               },
///               "PreviouslyInDesiredState":"false",
///               "RebootRequired":"false",
///               "ResultInformation":
///               {
///                   "ResultCode":"0",
///                   "Description":"Error description",
///                   "Details":"More Error Details",
///                   "ResultSource":"UnitProcessing"
///               }
///             }
///           ]
///       }
///   }
/// }
/// </summary>
internal sealed class ConfigureResponse : ResponseBase
{
    private readonly ApplyConfigurationResult _applyConfigurationResult;

    public ConfigureResponse(string requestId, IApplyConfigurationResult applyConfigurationResult)
        : base(requestId, "Configure")
    {
        _applyConfigurationResult = new ApplyConfigurationResult().Populate(applyConfigurationResult);
        GenerateJsonData();
    }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();

        var result = JsonSerializer.Serialize(_applyConfigurationResult);
        JsonData![nameof(ApplyConfigurationResult)] = result;
    }
}
