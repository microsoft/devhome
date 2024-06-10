// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

namespace WindowsSandboxExtension.Helpers;

internal sealed class Resources
{
    private static ResourceLoader? _resourceLoader;

    public static string GetResource(string identifier, ILogger? log = null, params object[] args)
    {
        try
        {
            if (_resourceLoader == null)
            {
                var path = ResourceLoader.GetDefaultResourceFilePath();
                _resourceLoader = new ResourceLoader(path);
            }

            var resourceStr = _resourceLoader.GetString(identifier);
            return string.Format(CultureInfo.CurrentCulture, resourceStr, args);
        }
        catch (Exception ex)
        {
            log?.Error(ex, $"Failed loading resource: {identifier}");

            // If we fail, load the original identifier so it is obvious which resource is missing.
            return identifier;
        }
    }
}
