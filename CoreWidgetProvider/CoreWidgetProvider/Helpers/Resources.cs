// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;
using Microsoft.Windows.ApplicationModel.Resources;

namespace CoreWidgetProvider.Helpers;
public static class Resources
{
    private static ResourceLoader? _resourceLoader;

    public static string GetResource(string identifier, Logger? log = null)
    {
        try
        {
            if (_resourceLoader == null)
            {
                _resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "Resources");
            }

            return _resourceLoader.GetString(identifier);
        }
        catch (Exception ex)
        {
            log?.ReportError($"Failed loading resource: {identifier}", ex);

            // If we fail, load the original identifier so it is obvious which resource is missing.
            return identifier;
        }
    }

    // Replaces all identifiers in the provided list in the target string. Assumes all identifiers
    // are wrapped with '%' to prevent sub-string replacement errors. This is intended for strings
    // such as a JSON string with resource identifiers embedded.
    public static string ReplaceIdentifers(string str, string[] resourceIdentifiers, Logger? log = null)
    {
        var start = DateTime.Now;
        foreach (var identifier in resourceIdentifiers)
        {
            // What is faster, String.Replace, RegEx, or StringBuilder.Replace? It is String.Replace().
            // https://learn.microsoft.com/en-us/archive/blogs/debuggingtoolbox/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance
            var resourceString = GetResource(identifier, log);
            str = str.Replace($"%{identifier}%", resourceString);
        }

        var elapsed = DateTime.Now - start;
        log?.ReportDebug($"Replaced identifiers in {elapsed.TotalMilliseconds}ms");
        return str;
    }

    // These are all the string identifiers that appear in widgets.
    public static string[] GetWidgetResourceIdentifiers()
    {
        return new string[]
        {
            "Widget_Template/Loading",
            "Widget_Template/SSHWallet",
            "Widget_Template/Target",
            "Widget_Template_Label/ConfigFile",
            "Widget_Template_Tooltip/Submit",
            "Widget_Template/ConfigFilePath",
            "Widget_Template/ConfigFileNotFound",
            "Widget_Template/NumOfHosts",
            "Widget_Template/EmptyHosts",
            "Widget_Template/Connect",
            "Widget_Template/ErrorProcessingConfigFile",
        };
    }
}
