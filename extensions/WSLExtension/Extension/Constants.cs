// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension;

public static class Constants
{
    public const string WindowsThumbnail = "ms-appx:///WslAssets/wsl-distro-default-image.jpg";

#if CANARY_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Canary/Files/WslAssets/wsl-provider-icon.png";
#elif STABLE_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome/Files/WslAssets/wsl-provider-icon.png";
#else
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Dev/Files/WslAssets/wsl-provider-icon.png";
#endif

    // Common strings used for WSL extension
    public const string WslProviderId = "Microsoft.WSL";
    public const string WindowsTerminalExe = "wt.exe";
    public const string WindowsTerminalPackageFamilyName = "Microsoft.WindowsTerminal_8wekyb3d8bbwe";
    public const string CommandPromptExe = "cmd.exe";
    public const string WslExe = "wsl.exe";
    public const string WslTemplateSubfolderName = "WslTemplates";

    public const string DefaultWslLogoPath = @"ms-appx:///WslAssets/wslLinux.png";
    public const string WslLogoPathFormat = @"ms-appx:///WslAssets/{0}";
    public const string KnownDistributionsLocalYamlLocation = @"ms-appx:///DistributionDefinitions/DistributionDefinition.yaml";
    public const string KnownDistributionsWebJsonLocation = @"https://aka.ms/wsldistributionsjson";

    // Wsl registry distributions location.
    public const string WslRegistryLocation = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

    // Wsl registry data names within a distribution location.
    public const string PackageFamilyRegistryName = "PackageFamilyName";
    public const string DistributionRegistryName = "DistributionName";
    public const string DefaultDistributionRegistryName = "DefaultDistribution";
    public const string WslVersion = "Version";
    public const string WslState = "State";
    public const int WslVersion2 = 2;
    public const int WslExeExitSuccess = 0;

    public static string LaunchDistributionArgs { get; } = "wsl --distribution {0}";

    // Unregisters wsl distribution from machine
    public const string UnregisterDistributionArgs = "--unregister {0}";

    // Terminate wsl distribution on the machine
    public const string TerminateDistributionArgs = "--terminate {0}";

    // Download, installs and registers wsl distribution on machine.
    public const string InstallDistributionArgs = "wsl --install {0}";

    // Gets list of all running distributions on machine
    public const string ListAllRunningDistributions = "--list --running";
}
