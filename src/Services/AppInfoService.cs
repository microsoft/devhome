// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Helpers;
using Windows.ApplicationModel;
using Windows.System.UserProfile;

namespace DevHome.Services;

public class AppInfoService : IAppInfoService
{
    public string IconPath { get; } = Path.Combine(AppContext.BaseDirectory, "Assets/DevHome.ico");

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
            return new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            return Assembly.GetExecutingAssembly().GetName().Version!;
        }
    }

    public string UserPreferredLanguage { get; } = GlobalizationPreferences.Languages.Count > 0 ? GlobalizationPreferences.Languages[0] : CultureInfo.CurrentCulture.Name;

    private static bool RunningAsAdmin
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
