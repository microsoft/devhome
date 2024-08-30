// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.ApplicationModel;

namespace DevHome.Services.Core.Contracts;

public enum TerminalHostKind
{
    Console,
    Terminal,
}

public interface ITerminalHost
{
    public bool CanBeExecuted();

    public Package PackageObj { get; }

    public string AbsolutePathOfExecutable { get; }

    public TerminalHostKind TerminalHostKind { get; }
}
