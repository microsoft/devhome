// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace WSLExtension;

public sealed class Constants
{
    public const string WindowsThumbnail = "ms-appx:///WSLExtension/Assets/wsl-distro-default-image.jpg";

#if CANARY_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Canary/Files/WSLExtension/Assets/wsl-provider-icon.png";
#elif STABLE_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome/Files/WSLExtension/Assets/wsl-provider-icon.png";
#else
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Dev/Files/WSLExtension/Assets/wsl-provider-icon.png";
#endif

    // Common strings used for WSL extension
    public const string WslModuleName = "WSL";
    public const string WslProviderDisplayName = "WSL";
    public const string WslProviderId = "WSL";
    public const string WindowsTerminalExe = "wt.exe";
    public const string WindowsTerminalPackageFamilyName = "Microsoft.WindowsTerminal_8wekyb3d8bbwe";
    public const string CommandPromptExe = "cmd.exe";
    public const string WslExe = "wsl.exe";

    public const string DefaultWslLogoPath = @"ms-appx:///WSLExtension/DistroDefinitions/Assets/linux.png";
    public const string WslLogoPathFormat = @"ms-appx:///WSLExtension/DistroDefinitions/Assets/{0}";
    public const string KnownDistributionYamlLocation = @"ms-appx:///WSLExtension/DistroDefinitions/KnownDistributionInfo.yaml";

    // Wsl registry distributions location.
    public const string WslRegisryLocation = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

    // Wsl registry data names within a distribution location.
    public const string PackageFamilyName = "PackageFamilyName";
    public const string DistributionName = "DistributionName";
    public const string WslVersion = "Version";
    public const string WslState = "State";

    // Launches wsl.exe with the provided distribution
    public const string LaunchDistributionArgs = "wsl --distribution {0}";

    // Unregisters wsl distribution from machine
    public const string UnregisterDistributionArgs = "--unregister {0}";

    // Download, installs and registers wsl distribution on machine.
    public const string InstallDistributionArgs = "--install {0} --web-download";

    // Download, installs and registers wsl distribution on machine.
    public const string ListAllWslDistributionsFromMsStoreArgs = "--list --online";
}
