// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Services.Core.Models;
using Windows.ApplicationModel;

namespace DevHome.Services.Core.Contracts;

public interface ITerminalService
{
    /// <summary>
    /// Gets the terminal host that the user has selected as their default terminal.
    /// </summary>
    /// <remarks>
    /// If the user has never selected a Windows terminal package as their default terminal, then
    /// Windows will be in the "Let Windows decide" state. If there is no terminal and
    /// console host information in the registry, there is no way to publicly retrieve the app that
    /// Windows will use. In this case either Console host or Windows terminal could be used. This
    /// method returns a <see cref="WindowsConsole"/> if Windows is in the "Let Windows decide" state
    /// or if the console host was selected as the users default.
    /// </remarks>
    /// <returns>
    /// An object that represents the default terminal host selected by the user.
    /// </returns>
    public Task<ITerminalHost> GetDefaultTerminalAsync();

    /// <summary>
    /// Gets the highest installed version of the Windows Terminal package (Release).
    /// </summary>
    /// <returns>The terminal package if it is installed. Null otherwise.</returns>
    public WindowsTerminal GetTerminalPackageIfInstalled();
}
