// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace DevHome.Common.Helpers;

public class GPOHelper
{
    private enum GpoRuleConfigured
    {
        WrongValue = -3, // The policy is set to an unrecognized value
        Unavailable = -2, // Couldn't access registry
        NotConfigured = -1, // Policy is not configured
        Disabled = 0, // Policy is disabled
        Enabled = 1, // Policy is enabled
    }

    // Registry path where gpo policy values are stored
    private const string PoliciesScopeMachine = "HKEY_LOCAL_MACHINE";
    private const string PoliciesPath = @"\SOFTWARE\Policies\DevHome";

    // Registry value names
    private const string PolicyConfigureEnabledDevHome = "ConfigureEnabledDevHome";
    private const string PolicyConfigureEnabledMachineConfiguration = "ConfigureEnabledMachineConfiguration";
    private const string PolicyConfigureEnabledEnvironments = "ConfigureEnabledEnvironments";
    private const string PolicyConfigureEnabledExperimentalFeatures = "ConfigureEnabledExperimentalFeatures";
    private const string PolicyConfigureHiddenDevHome = "ConfigureHiddenDevHome";

    private static GpoRuleConfigured GetConfiguredValue(string registryValueName)
    {
        try
        {
            var rawValue = Registry.GetValue(
                keyName: PoliciesScopeMachine + PoliciesPath,
                valueName: registryValueName,
                defaultValue: GpoRuleConfigured.NotConfigured);

            // Value will be null if the subkey specified by keyName does not exist.
            if (rawValue == null)
            {
                return GpoRuleConfigured.NotConfigured;
            }
            else if (rawValue is not int)
            {
                return GpoRuleConfigured.WrongValue;
            }
            else
            {
                return (GpoRuleConfigured)rawValue;
            }
        }
        catch (System.Security.SecurityException)
        {
            // The user does not have the permissions required to read from the registry key.
            return GpoRuleConfigured.Unavailable;
        }
        catch (System.IO.IOException)
        {
            // The RegistryKey that contains the specified value has been marked for deletion.
            return GpoRuleConfigured.Unavailable;
        }
        catch (System.ArgumentException)
        {
            // keyName does not begin with a valid registry root.
            return GpoRuleConfigured.NotConfigured;
        }
    }

    private static bool EvaluateConfiguredValue(string registryValueName, GpoRuleConfigured defaultValue)
    {
        var configuredValue = GetConfiguredValue(registryValueName);
        if (configuredValue < 0)
        {
            configuredValue = defaultValue;
        }

        return configuredValue == GpoRuleConfigured.Enabled;
    }

    public static bool GetConfiguredEnabledDevHomeValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledDevHome, defaultValue);
    }

    public static bool GetConfiguredEnabledMachineConfigurationValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledMachineConfiguration, defaultValue);
    }

    public static bool GetConfiguredEnabledEnvironmentsValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledEnvironments, defaultValue);
    }

    public static bool GetConfiguredEnabledExperimentalFeaturesValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledExperimentalFeatures, defaultValue);
    }

    public static bool GetConfiguredHiddenDevHomeValue()
    {
        var defaultValue = GpoRuleConfigured.Disabled;
        return EvaluateConfiguredValue(PolicyConfigureHiddenDevHome, defaultValue);
    }
}
