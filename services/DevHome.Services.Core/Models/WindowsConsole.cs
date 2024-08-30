// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Services.Core.Contracts;
using Windows.ApplicationModel;

namespace DevHome.Services.Core.Models;

public sealed class WindowsConsole : ITerminalHost
{
    private readonly string _commandPromptExe = "cmd.exe";

    public string AbsolutePathOfExecutable => @$"{Environment.SystemDirectory}\{_commandPromptExe}";

    public bool CanBeExecuted() => true;

    public Package PackageObj => null;

    public TerminalHostKind TerminalHostKind => TerminalHostKind.Console;
}
