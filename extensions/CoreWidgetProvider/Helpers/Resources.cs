// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

namespace CoreWidgetProvider.Helpers;

public static class Resources
{
    private static ResourceLoader? _resourceLoader;

    public static string GetResource(string identifier, ILogger? log = null)
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
            log?.Error(ex, $"Failed loading resource: {identifier}");

            // If we fail, load the original identifier so it is obvious which resource is missing.
            return identifier;
        }
    }

    // Replaces all identifiers in the provided list in the target string. Assumes all identifiers
    // are wrapped with '%' to prevent sub-string replacement errors. This is intended for strings
    // such as a JSON string with resource identifiers embedded.
    public static string ReplaceIdentifers(string str, string[] resourceIdentifiers, ILogger? log = null)
    {
        var start = DateTime.Now;
        foreach (var identifier in resourceIdentifiers)
        {
            // What is faster, String.Replace, RegEx, or StringBuilder.Replace? It is String.Replace().
            // https://learn.microsoft.com/archive/blogs/debuggingtoolbox/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance
            var resourceString = GetResource(identifier, log);
            str = str.Replace($"%{identifier}%", resourceString);
        }

        var elapsed = DateTime.Now - start;
        log?.Debug($"Replaced identifiers in {elapsed.TotalMilliseconds}ms");
        return str;
    }

    // These are all the string identifiers that appear in widgets.
    public static string[] GetWidgetResourceIdentifiers()
    {
        return
        [
            "Widget_Template/Loading",
            "Widget_Template_Tooltip/Submit",
            "SSH_Widget_Template/Name",
            "SSH_Widget_Template/Target",
            "SSH_Widget_Template/ConfigFilePath",
            "SSH_Widget_Template/ConfigFileNotFound",
            "SSH_Widget_Template/NumOfHosts",
            "SSH_Widget_Template/EmptyHosts",
            "SSH_Widget_Template/Connect",
            "SSH_Widget_Template/ErrorProcessingConfigFile",
            "Memory_Widget_Template/SystemMemory",
            "Memory_Widget_Template/MemoryUsage",
            "Memory_Widget_Template/AllMemory",
            "Memory_Widget_Template/UsedMemory",
            "Memory_Widget_Template/Committed",
            "Memory_Widget_Template/Cached",
            "Memory_Widget_Template/NonPagedPool",
            "Memory_Widget_Template/PagedPool",
            "NetworkUsage_Widget_Template/Network_Usage",
            "NetworkUsage_Widget_Template/Sent",
            "NetworkUsage_Widget_Template/Received",
            "NetworkUsage_Widget_Template/Network_Name",
            "NetworkUsage_Widget_Template/Previous_Network",
            "NetworkUsage_Widget_Template/Next_Network",
            "NetworkUsage_Widget_Template/Ethernet_Heading",
            "GPUUsage_Widget_Template/GPU_Usage",
            "GPUUsage_Widget_Template/GPU_Name",
            "GPUUsage_Widget_Template/GPU_Temperature",
            "GPUUsage_Widget_Template/Previous_GPU",
            "GPUUsage_Widget_Template/Next_GPU",
            "CPUUsage_Widget_Template/CPU_Usage",
            "CPUUsage_Widget_Template/CPU_Speed",
            "CPUUsage_Widget_Template/Processes",
            "CPUUsage_Widget_Template/End_Process",
            "Widget_Template_Button/Save",
            "Widget_Template_Button/Cancel",
        ];
    }
}
