// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.HostGuestCommunication;
using Microsoft.Windows.DevHome.DevSetupEngine;

namespace HyperVExtension.DevSetupAgent;

public static class ApplyConfigurationResultExtensions
{
    public static ApplyConfigurationResult Populate(
        this ApplyConfigurationResult result, IApplyConfigurationResult applyConfigurationResult)
    {
        if (applyConfigurationResult.ResultCode != null)
        {
            result.ResultCode = applyConfigurationResult.ResultCode.HResult;
        }

        result.ResultDescription = applyConfigurationResult.ResultDescription;
        result.OpenConfigurationSetResult = new OpenConfigurationSetResult().Populate(applyConfigurationResult.OpenConfigurationSetResult);
        result.ApplyConfigurationSetResult = new ApplyConfigurationSetResult().Populate(applyConfigurationResult.ApplyConfigurationSetResult);
        return result;
    }

    public static ConfigurationUnitResultInformation Populate(
        this ConfigurationUnitResultInformation result, IConfigurationUnitResultInformation? configurationUnitResultInformation)
    {
        if (configurationUnitResultInformation != null)
        {
            result.ResultCode = configurationUnitResultInformation.ResultCode != null ? configurationUnitResultInformation.ResultCode.HResult : 0;
            result.Description = configurationUnitResultInformation.Description;
            result.Details = configurationUnitResultInformation.Details;
            result.ResultSource = (HostGuestCommunication.ConfigurationUnitResultSource)configurationUnitResultInformation.ResultSource;
        }

        return result;
    }

    public static OpenConfigurationSetResult Populate(this OpenConfigurationSetResult result, IOpenConfigurationSetResult? openConfigurationSetResult)
    {
        if (openConfigurationSetResult != null)
        {
            result.ResultCode = openConfigurationSetResult.ResultCode != null ? openConfigurationSetResult.ResultCode.HResult : 0;
            result.Field = openConfigurationSetResult.Field;
            result.Value = openConfigurationSetResult.Value;
            result.Line = openConfigurationSetResult.Line;
            result.Column = openConfigurationSetResult.Column;
        }

        return result;
    }

    public static ConfigurationUnit Populate(this ConfigurationUnit result, IConfigurationUnit? configurationUnit)
    {
        if (configurationUnit != null)
        {
            result.Type = configurationUnit.Type;
            result.Identifier = configurationUnit.Identifier;
            result.State = (HostGuestCommunication.ConfigurationUnitState)configurationUnit.State;
            result.Intent = (HostGuestCommunication.ConfigurationUnitIntent)configurationUnit.Intent;
            result.IsGroup = configurationUnit.IsGroup;
            if (configurationUnit.Settings != null)
            {
                result.Settings = new Dictionary<string, string>();
                foreach (var setting in configurationUnit.Settings)
                {
                    result.Settings.Add(setting.Key, setting.Value.ToString() ?? string.Empty);
                }
            }

            if (configurationUnit.Units != null)
            {
                var count = configurationUnit.Units.Count;
                if (count > 0)
                {
                    result.Units = new List<ConfigurationUnit>(count);
                    for (var i = 0; i < count; i++)
                    {
                        result.Units.Add(new ConfigurationUnit().Populate(configurationUnit.Units[i]));
                    }
                }
            }
        }

        return result;
    }

    public static ConfigurationSetChangeData Populate(this ConfigurationSetChangeData result, IConfigurationSetChangeData? configurationSetChangeData)
    {
        if (configurationSetChangeData != null)
        {
            result.Change = (HostGuestCommunication.ConfigurationSetChangeEventType)configurationSetChangeData.Change;
            result.SetState = (HostGuestCommunication.ConfigurationSetState)configurationSetChangeData.SetState;
            result.UnitState = (HostGuestCommunication.ConfigurationUnitState)configurationSetChangeData.UnitState;
            if (configurationSetChangeData.ResultInformation != null)
            {
                result.ResultInformation = new ConfigurationUnitResultInformation().Populate(configurationSetChangeData.ResultInformation);
            }

            if (configurationSetChangeData.Unit != null)
            {
                result.Unit = new ConfigurationUnit().Populate(configurationSetChangeData.Unit);
            }
        }

        return result;
    }

    public static ApplyConfigurationUnitResult Populate(this ApplyConfigurationUnitResult result, IApplyConfigurationUnitResult? applyConfigurationUnitResult)
    {
        if (applyConfigurationUnitResult != null)
        {
            if (applyConfigurationUnitResult.Unit != null)
            {
                result.Unit = new ConfigurationUnit().Populate(applyConfigurationUnitResult.Unit);
            }

            result.PreviouslyInDesiredState = applyConfigurationUnitResult.PreviouslyInDesiredState;
            result.RebootRequired = applyConfigurationUnitResult.RebootRequired;
            if (applyConfigurationUnitResult.ResultInformation != null)
            {
                result.ResultInformation = new ConfigurationUnitResultInformation().Populate(applyConfigurationUnitResult.ResultInformation);
            }
        }

        return result;
    }

    public static ApplyConfigurationSetResult Populate(this ApplyConfigurationSetResult result, IApplyConfigurationSetResult applyConfigurationSetResult)
    {
        result.ResultCode = applyConfigurationSetResult?.ResultCode?.HResult ?? 0;
        if (applyConfigurationSetResult?.UnitResults != null)
        {
            var count = applyConfigurationSetResult.UnitResults.Count;
            if (count > 0)
            {
                result.UnitResults = new List<ApplyConfigurationUnitResult>(count);
                for (var i = 0; i < count; i++)
                {
                    result.UnitResults.Add(new ApplyConfigurationUnitResult().Populate(applyConfigurationSetResult.UnitResults[i]));
                }
            }
        }

        return result;
    }
}
