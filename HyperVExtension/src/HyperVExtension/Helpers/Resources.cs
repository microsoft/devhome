// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

namespace HyperVExtension.Helpers;

public static class Resources
{
    private static ResourceLoader? _resourceLoader;

    public static string GetResource(string identifier, ILogger? log = null)
    {
        try
        {
            if (_resourceLoader == null)
            {
                var path = ResourceLoader.GetDefaultResourceFilePath();
                _resourceLoader = new ResourceLoader(path, "HyperVExtension/Resources");
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

    public static string[] GetHyperVResourceIdentifiers()
    {
        return
        [
            "VmCredentialRequest/Title",
            "VmCredentialRequest/Description1",
            "VmCredentialRequest/Description2",
            "VmCredentialRequest/UsernameErrorMsg",
            "VmCredentialRequest/PasswordErrorMsg",
            "VmCredentialRequest/UsernameLabel",
            "VmCredentialRequest/PasswordLabel",
            "VmCredentialRequest/OkText",
            "VmCredentialRequest/CancelText",
            "WaitForLoginRequest/Title",
            "WaitForLoginRequest/Description",
        ];
    }
}
