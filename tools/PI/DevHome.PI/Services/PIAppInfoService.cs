// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using DevHome.Common.Helpers;
using Windows.ApplicationModel;

namespace DevHome.PI.Services;

public class PIAppInfoService
{
    public string IconPath { get; } = Path.Combine(AppContext.BaseDirectory, "Images/PI.ico");

    public Version GetAppVersion()
    {
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            return new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            return Assembly.GetExecutingAssembly().GetName().Version!;
        }
    }
}
