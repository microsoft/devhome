// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using WSLExtension.Models;
using static Microsoft.Win32.RegistryKey;

namespace WSLExtension.Services;

public class RegistryAccess : IRegistryAccess
{
    public string? GetBasePath(string distroRegistration)
    {
        var baseKey = OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        try
        {
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss", false);

            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var subKey = key.OpenSubKey(subKeyName);
                    var regDistributionName = (string?)subKey?.GetValue("DistributionName");
                    if (distroRegistration == regDistributionName)
                    {
                        return (string?)subKey?.GetValue("BasePath");
                    }
                }
            }
        }
        finally
        {
            baseKey.Dispose();
        }

        return null;
    }

    public string? GetWindowsVersion()
    {
        var baseKey = OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        try
        {
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            return (string?)key?.GetValue("CurrentBuild");
        }
        finally
        {
            baseKey.Dispose();
        }
    }

    public void SetDistroDefaultUser(string distroRegistration, string defaultUId)
    {
        var baseKey = OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        try
        {
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss");
            if (key == null)
            {
                return;
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                var subKey = key.OpenSubKey(subKeyName);
                var regDistributionName = (string?)subKey?.GetValue("DistributionName");
                if (distroRegistration == regDistributionName)
                {
                    /* Inside appx registry write goes to a private file,
                         calling the command ensures the proper registry write*/
                    new ProcessCaller().CallProcess(
                        "reg",
                        $"add {subKey?.Name} /v DefaultUId /t REG_DWORD /d {defaultUId} /f");
                }
            }
        }
        finally
        {
            baseKey.Dispose();
        }
    }

    public int? GetDefaultWslVersion()
    {
        var baseKey = OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        try
        {
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss");

            var value = key?.GetValue("DefaultVersion");

            return (int?)value;
        }
        finally
        {
            baseKey.Dispose();
        }
    }

    public int GetWindowsTerminalVersion()
    {
        var packageManager = new PackageManager();

        IEnumerable<Package>? packages =
            packageManager.FindPackagesForUser(string.Empty, "Microsoft.WindowsTerminal_8wekyb3d8bbwe");
        var version = packages.Select(CalculateVersion).FirstOrDefault();

        /* Check for Windows Terminal Preview */

        // ReSharper disable once InvertIf
        if (version == default)
        {
            packages = packageManager.FindPackagesForUser(string.Empty, "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe");
            version = packages.Select(CalculateVersion).FirstOrDefault();
        }

        return version;

        static int CalculateVersion(Package package)
        {
            var packageVersion = package.Id.Version;

            return (packageVersion.Major * 100) + (packageVersion.Minor * 10);
        }
    }

    public IList<Distro> GetInstalledDistros()
    {
        var distros = new List<Distro>();

        var baseKey = OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        try
        {
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss", false);

            if (key != null)
            {
                var defaultDistribution = (string?)key.GetValue("DefaultDistribution");
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var subKey = key.OpenSubKey(subKeyName);

                    if (subKey == null)
                    {
                        continue;
                    }

                    var value = subKey.GetValue("State");
                    if (value == null || (int)value != 1)
                    {
                        continue;
                    }

                    var regDistributionName = (string?)subKey.GetValue("DistributionName");
                    if (regDistributionName == null)
                    {
                        continue;
                    }

                    var distro = new Distro(regDistributionName);
                    distros.Add(distro);

                    if (subKeyName == defaultDistribution)
                    {
                        distro.DefaultDistro = true;
                    }

                    var version = subKey.GetValue("Version");
                    if (version != null)
                    {
                        distro.Version2 = (int)version == 2;
                    }
                }
            }
        }
        finally
        {
            baseKey.Dispose();
        }

        return distros;
    }
}
