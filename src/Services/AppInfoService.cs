// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Reflection;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Helpers;
using Windows.ApplicationModel;

namespace DevHome.Services;
public class AppInfoService : IAppInfoService
{
    public string GetAppNameLocalized()
    {
#if CANARY_BUILD
        return "AppDisplayNameCanary".GetLocalized();
#elif STABLE_BUILD
        return "AppDisplayNameStable".GetLocalized();
#else
        return "AppDisplayNameDev".GetLocalized();
#endif
    }

    public Version GetAppVersion()
    {
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            return new (packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            return Assembly.GetExecutingAssembly().GetName().Version!;
        }
    }
}
