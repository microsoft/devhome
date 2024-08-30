// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Services.Core.Contracts;
using DevHome.Services.Core.Models;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation.Collections;
using static Microsoft.Win32.Registry;

namespace DevHome.Services.Core.Services;

/// <summary>
/// Used to query and retrieve the terminal app that the user has selected as their default for Windows.
/// </summary>
/// <remarks>
/// Code created based on Delegation.cpp file in the Windows Terminal repository.
/// https://github.com/microsoft/terminal/blob/17a55da0f9889aafd84df024299982e7b94f5482/src/propslib/DelegationConfig.cpp#L33
/// When users update their default terminal host, two entries are written to the following registry location:
/// "HKEY_CURRENT_USER\Console\%%Startup". One Guid entry for the DelegationConsole and one for the DelegationTerminal.
/// Both are used by Windows to find the default Terminal app. These Guids are found via the Windows Terminal package
/// manifest or internally assigned by Windows e.g When users select 'Windows Console Host' the
/// <see cref="WindowsConsoleDelegationClsid"/> is selected as the DelegationConsole and the DelegationTerminal.
/// </remarks>
public class TerminalService : ITerminalService
{
    private readonly ILogger _log;

    private const string TerminalDelegationName = "DelegationTerminal";

    private const string ConsoleDelegationName = "DelegationConsole";

    private const string ClsidPropertyName = "Clsid";

    private const string ClsidManifestPropertyKey = "#text";

    private const string TerminalAppExtension = "com.microsoft.windows.terminal.host";

    private const string ConHostAppExtension = "com.microsoft.windows.console.host";

    private const string ConsoleStartupRegistryLocation = @"Console\%%Startup";

    public const string WindowsTerminalPackageFamilyName = "Microsoft.WindowsTerminal_8wekyb3d8bbwe";

    private const string DefaultDelegationClsid = "{00000000-0000-0000-0000-000000000000}";

    private const string WindowsConsoleDelegationClsid = "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}";

    private readonly IPackageDeploymentService _packageService;

    public TerminalService(IPackageDeploymentService packageService, ILogger<TerminalService> logger)
    {
        _packageService = packageService;
        _log = logger;
    }

    /// <inheritdoc cref="ITerminalService.GetDefaultTerminalAsync"/>
    public async Task<ITerminalHost> GetDefaultTerminalAsync()
    {
        var consoleStartupKey = CurrentUser.OpenSubKey(ConsoleStartupRegistryLocation, false);

        // The user has never updated their terminal settings or launched terminal so it is set to
        // "Let Windows decide". By default its either Windows terminal or regular console host.
        // There are no direct APIs to tell what the default is.
        if (consoleStartupKey == null)
        {
            return new WindowsConsole();
        }

        // Console startup registry location exists but there are no entries for their terminal
        // delegation or the console delegation. So treat this the same as letting Windows choose
        // by default.
        if (consoleStartupKey.GetValue(TerminalDelegationName) is not string terminalDelegationId ||
            consoleStartupKey.GetValue(ConsoleDelegationName) is not string consoleDelegationId)
        {
            return new WindowsConsole();
        }

        // There are cases where in the startup console location both delegations have their clsid's
        // set to "{00000000-0000-0000-0000-000000000000}". This means the same thing as
        // "Let Windows decide" so we return null here.
        if (terminalDelegationId.Equals(DefaultDelegationClsid, StringComparison.OrdinalIgnoreCase) ||
            consoleDelegationId.Equals(DefaultDelegationClsid, StringComparison.OrdinalIgnoreCase))
        {
            return new WindowsConsole();
        }

        // The user has the "Windows Console Host" selected as their Default terminal host.
        if (terminalDelegationId.Equals(WindowsConsoleDelegationClsid, StringComparison.OrdinalIgnoreCase) ||
            consoleDelegationId.Equals(WindowsConsoleDelegationClsid, StringComparison.OrdinalIgnoreCase))
        {
            return new WindowsConsole();
        }

        var terminalExCatalog = AppExtensionCatalog.Open(TerminalAppExtension);
        var terminalExtensions = await terminalExCatalog.FindAllAsync();
        var conHostExCatalog = AppExtensionCatalog.Open(ConHostAppExtension);
        var conHostExtensions = await conHostExCatalog.FindAllAsync();

        // If the classId for the terminal app extension and the console host app extension
        // matches what we found in the registry then we've found the users default terminal app.
        foreach (var terminal in terminalExtensions)
        {
            var terminalClsid = await GetClsidPropertyFromManifest(terminal);

            if (terminalClsid == null ||
                !terminalDelegationId.Equals(terminalClsid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var conHost in conHostExtensions)
            {
                var conHostClsid = await GetClsidPropertyFromManifest(conHost);

                if (conHostClsid != null &&
                    consoleDelegationId.Equals(conHostClsid, StringComparison.OrdinalIgnoreCase))
                {
                    return new WindowsTerminal(terminal.Package);
                }
            }
        }

        // All the cases should be covered above. If we get here then it could be that the terminal
        // and conhost app extensions changed where the Clsid properties are located which is unlikely.
        // See terminal code here where this is based off of.
        _log.LogError("Couldn't find default Terminal host based on registry information");
        return new WindowsConsole();
    }

    /// <inheritdoc cref="ITerminalService.GetTerminalPackageIfInstalled"/>
    public WindowsTerminal GetTerminalPackageIfInstalled()
    {
        var terminalPackage = _packageService.FindPackagesForCurrentUser(WindowsTerminalPackageFamilyName);
        return new WindowsTerminal(terminalPackage.FirstOrDefault());
    }

    private async Task<string> GetClsidPropertyFromManifest(AppExtension appExtension)
    {
        var appExtensionPropertySet = await appExtension.GetExtensionPropertiesAsync();
        appExtensionPropertySet.TryGetValue(ClsidPropertyName, out var clsidMap);

        if (clsidMap is not IPropertySet clsidPropSet)
        {
            return string.Empty;
        }

        clsidPropSet.TryGetValue(ClsidManifestPropertyKey, out var clsidGuid);
        return clsidGuid as string;
    }
}
