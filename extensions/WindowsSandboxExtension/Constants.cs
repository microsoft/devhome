// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsSandboxExtension;

internal sealed class Constants
{
    public const string ProviderId = "Microsoft.WindowsSandbox";
    public const string ProviderDisplayName = "Windows Sandbox";
    public const string ComputeSystemName = "Lightweight Desktop Environment";
    public const string Thumbnail = "ms-appx:///Assets/windows-sandbox-thumbnail.jpg";

    // We use different icon locations for different builds. Note these are ms-resource URIs, but are used by Dev Home to load the providers icon.
    // from the extension package. Extensions that implement the IComputeSystemProvider interface must provide a provider icon in this format.
    // Dev Home will use SHLoadIndirectString (https://learn.microsoft.com/windows/win32/api/shlwapi/nf-shlwapi-shloadindirectstring) to load the
    // location of the icon from the extension package.Once it gets this location, it will load the icon from the path and display it in the UI.
    // Icons should be located in an extension resource.pri file which is generated at build time.
    // See the MakePri.exe documentation for how you can view what is in the resource.pri file, so you can find the location of your icon.
    // https://learn.microsoft.com/windows/uwp/app-resources/makepri-exe-command-options. (use MakePri.exe in a VS Developer Command Prompt or
    // Powershell window)
#if CANARY_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Canary/Files/Assets/windows-sandbox-icon.png";
#elif STABLE_BUILD
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome/Files/Assets/windows-sandbox-icon.png";
#else
    public const string ExtensionIcon = "ms-resource://Microsoft.Windows.DevHome.Dev/Files/Assets/windows-sandbox-icon.png";
#endif
}
