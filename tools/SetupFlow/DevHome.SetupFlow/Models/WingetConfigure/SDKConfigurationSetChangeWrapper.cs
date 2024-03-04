// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.SetupFlow.Services;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKConfigurationSetChangeWrapper
{
    private readonly ISetupFlowStringResource _stringResource;

    private readonly SDK.ConfigurationSetChangeData _configurationSetChangeData;

    private string Id { get; set; }

    public SDKConfigurationSetChangeWrapper(SDK.ConfigurationSetChangeData configurationUnitState, ISetupFlowStringResource setupFlowStringResource)
    {
        _configurationSetChangeData = configurationUnitState;
        _stringResource = setupFlowStringResource;

        ShortFailureDescription = ResultInformation?.Description;
        DetailedFailureDescription = ResultInformation?.Details;
    }

    // The change event type that occurred.
    public SDK.ConfigurationSetChangeEventType Change => _configurationSetChangeData.Change;

    // The state of the configuration set for this event (the ConfigurationSet can be used to get the current state, which may be different).
    public SDK.ConfigurationSetState SetState => _configurationSetChangeData.SetState;

    // The state of the configuration unit for this event (the ConfigurationUnit can be used to get the current state, which may be different).
    public SDK.ConfigurationUnitState UnitState => _configurationSetChangeData.UnitState;

    // Contains information on the result of the attempt to apply the configuration unit.
    public SDK.ConfigurationUnitResultInformation ResultInformation => _configurationSetChangeData.ResultInformation;

    // The result from opening the set.
    public string DetailedFailureDescription { get; private set; }

    public string ShortFailureDescription { get; private set; }

    // The configuration unit whose state changed.
    public SDK.ConfigurationUnit Unit => _configurationSetChangeData.Unit;

    public bool IsErrorMessagePresent => ResultInformation != null && (!string.IsNullOrEmpty(DetailedFailureDescription) || !string.IsNullOrEmpty(ShortFailureDescription));

    public string ConfigurationSetState
    {
        get
        {
            switch (SetState)
            {
                case SDK.ConfigurationSetState.Unknown:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnknown);
                case SDK.ConfigurationSetState.Pending:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationPending);
                case SDK.ConfigurationSetState.InProgress:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationInProgress);
                case SDK.ConfigurationSetState.Completed:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationCompleted);
                case SDK.ConfigurationSetState.ShuttingDownDevice:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationShuttingDownDevice);
                case SDK.ConfigurationSetState.StartingDevice:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationStartingDevice);
                case SDK.ConfigurationSetState.RestartingDevice:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationRestartingDevice);
                case SDK.ConfigurationSetState.ProvisioningDevice:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationProvisioningDevice);
                case SDK.ConfigurationSetState.WaitingForAdminUserLogon:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationWaitingForAdminUserLogon);
                case SDK.ConfigurationSetState.WaitingForUserLogon:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationWaitingForUserLogon);
                default:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnknown);
            }
        }
    }

    public string ConfigurationUnitState
    {
        get
        {
            switch (UnitState)
            {
                case SDK.ConfigurationUnitState.Unknown:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitUnknown);
                case SDK.ConfigurationUnitState.Pending:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitPending);
                case SDK.ConfigurationUnitState.InProgress:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitInProgress);
                case SDK.ConfigurationUnitState.Completed:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitCompleted);
                case SDK.ConfigurationUnitState.Skipped:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitSkipped);
                default:
                    return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitUnknown);
            }
        }
    }

    public override string ToString()
    {
        var configSetName = _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationSetProgressMessage, SetState);
        var configUnitName = _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitProgressMessage, ConfigurationUnitState);

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(CultureInfo.CurrentCulture, $"{configSetName} ");
        stringBuilder.AppendLine(CultureInfo.CurrentCulture, $"{configUnitName} ");

        return stringBuilder.ToString();
    }

    public string GetErrorMessagesForDisplay()
    {
        var resourceName = Unit?.Type ?? _stringResource.GetLocalized(StringResourceKey.SetupTargetUnknownStatus);

        if (string.IsNullOrEmpty(ShortFailureDescription))
        {
            return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitProgressError, resourceName);
        }

        return _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationUnitProgressErrorWithMsg, resourceName, ShortFailureDescription);
    }

    public string GetErrorMessageForLogging()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Short error description: {ShortFailureDescription} ");
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Detailed error description: {DetailedFailureDescription} ");

        return stringBuilder.ToString();
    }
}
