// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace WSLExtension;

public sealed class Constants
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
    public const string WslProviderDisplayName = "WSL";
    public const string WslProviderId = "Microsoft.WSL";
    public const string WindowsTerminalExe = "wt.exe";
    public const string WindowsTerminalPackageFamilyName = "Microsoft.WindowsTerminal_8wekyb3d8bbwe";
    public const string CommandPromptExe = "cmd.exe";
    public const string WslExe = "wsl.exe";

    public const string DefaultWslLogoPath = @"ms-appx:///WslAssets/wslLinux.png";
    public const string WslLogoPathFormat = @"ms-appx:///WslAssets/{0}";
    public const string KnownDistributionsLocalYamlLocation = @"ms-appx:///DistributionDefinitions/DistributionDefinition.yaml";
    public const string KnownDistributionsWebJsonLocation = @"https://raw.githubusercontent.com/microsoft/WSL/master/distributions/DistributionInfo.json";

    // Wsl registry distributions location.
    public const string WslRegistryLocation = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

    // Wsl registry data names within a distribution location.
    public const string PackageFamilyName = "PackageFamilyName";
    public const string DistributionName = "DistributionName";
    public const string WslVersion = "Version";
    public const string WslState = "State";
    public const int WslVersion2 = 2;

    // Launches wsl.exe with the provided distribution
    public const string LaunchDistributionArgs = "wsl --distribution {0}";

    // Unregisters wsl distribution from machine
    public const string UnregisterDistributionArgs = "--unregister {0}";

    // Download, installs and registers wsl distribution on machine.
    public const string InstallDistributionArgs = "--install {0} --web-download";

    // Download, installs and registers wsl distribution on machine.
    public const string ListAllWslDistributionsFromMsStoreArgs = "--list --online";
}
