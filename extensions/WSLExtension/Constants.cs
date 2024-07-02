// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension;

public static class Constants
{
#if CANARY_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Canary/Files/WslAssets/wslLinux.png";
#elif STABLE_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome/Files/WslAssets/wslLinux.png";
#else
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Dev/Files/WslAssets/wslLinux.png";
#endif

    // Common strings used for WSL extension
    public const string WslProviderDisplayName = "Microsoft WSL";
    public const string WslProviderId = "Microsoft.WSL";
    public const string WindowsTerminalShimExe = "wt.exe";
    public const string WindowsTerminalPackageFamilyName = "Microsoft.WindowsTerminal_8wekyb3d8bbwe";
    public const string CommandPromptExe = "cmd.exe";
    public const string WslExe = "wsl.exe";
    public const string WslTemplateSubfolderName = "WslTemplates";

    public const string DefaultWslLogoPath = @"ms-appx:///WslAssets/wslLinux.png";
    public const string WslLogoPathFormat = @"ms-appx:///WslAssets/{0}";
    public const string KnownDistributionsLocalYamlLocation = @"ms-appx:///DistributionDefinitions/DistributionDefinition.yaml";
    public const string KnownDistributionsWebJsonLocation = @"https://aka.ms/wsldistributionsjson";

    // Wsl registry location for registered distributions.
    public const string WslRegistryLocation = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

    // Wsl registry data names within a distribution location.
    public const string PackageFamilyRegistryName = "PackageFamilyName";
    public const string DistributionRegistryName = "DistributionName";
    public const string DefaultDistributionRegistryName = "DefaultDistribution";
    public const string WslVersion = "Version";
    public const string WslState = "State";
    public const int WslVersion1 = 1;
    public const int WslVersion2 = 2;
    public const int WslExeExitSuccess = 0;

    // Launch terminal with specific profile and log the user into their home directory in the login shell
    public static string LaunchDistributionProfileArgs { get; } = "--profile {0} -- wsl --shell-type login --cd ~ --distribution {1}";

    // Launch without using a Terminal profile and log the user into their home directory using the login shell
    public static string LaunchDistributionNoProfileArgs { get; } = "wsl --shell-type login --cd ~ --distribution {0}";

    // Arguments to unregister a wsl distribution from a machine using wsl.exe
    public const string UnregisterDistributionArgs = "--unregister {0}";

    // Arguments to terminate all wsl sessions for a specific distribution using wsl.exe
    public const string TerminateDistributionArgs = "--terminate {0}";

    // Arguments to download, install and register a wsl distribution using wsl.exe
    public const string InstallDistributionArgs = "wsl --install {0}";

    // Arguments to list of all running distributions on a machine using wsl.exe
    public const string ListAllRunningDistributions = "--list --running";
}
