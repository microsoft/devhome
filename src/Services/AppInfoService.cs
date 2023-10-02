// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Reflection;
using System.Security.Principal;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Helpers;
using Windows.ApplicationModel;

namespace DevHome.Services;
public class AppInfoService : IAppInfoService
{
    private static bool RunningAsAdmin
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public string GetAppNameLocalized()
    {
#if CANARY_BUILD
        return RunningAsAdmin ? "AppDisplayNameCanaryAdministrator".GetLocalized() : "AppDisplayNameCanary".GetLocalized();
#elif STABLE_BUILD
        return RunningAsAdmin ? "AppDisplayNameStableAdministrator".GetLocalized() : "AppDisplayNameStable".GetLocalized();
#else
        return RunningAsAdmin ? "AppDisplayNameDevAdministrator".GetLocalized() : "AppDisplayNameDev".GetLocalized();
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
