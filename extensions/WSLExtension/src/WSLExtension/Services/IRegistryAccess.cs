// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using WSLExtension.Models;

namespace WSLExtension.Services;

public interface IRegistryAccess
{
    string? GetBasePath(string distroRegistration);

    string? GetWindowsVersion();

    void SetDistroDefaultUser(string distroRegistration, string defaultUId);

    IList<Distro> GetInstalledDistros();

    int? GetDefaultWslVersion();

    int GetWindowsTerminalVersion();
}
