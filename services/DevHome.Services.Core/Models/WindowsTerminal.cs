// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Services.Core.Contracts;
using Windows.ApplicationModel;

namespace DevHome.Services.Core.Models;

public class WindowsTerminal : ITerminalHost
{
    private const string WindowsTerminalShimExe = "wt.exe";

    public Package PackageObj { get; }

    public string AbsolutePathOfExecutable { get; } = string.Empty;

    public TerminalHostKind TerminalHostKind => TerminalHostKind.Terminal;

    public WindowsTerminal(Package package)
    {
        PackageObj = package;

        if (PackageObj != null)
        {
            var appdataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AbsolutePathOfExecutable = @$"{appdataLocal}\Microsoft\WindowsApps\{PackageObj.Id.FamilyName}\{WindowsTerminalShimExe}";
        }
    }

    public bool CanBeExecuted()
    {
        return PackageObj != null && !PackageObj.IsStub;
    }
}
