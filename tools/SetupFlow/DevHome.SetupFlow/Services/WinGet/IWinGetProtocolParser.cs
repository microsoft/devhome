// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services.WinGet;
public interface IWinGetProtocolParser
{
    /// <summary>
    /// Get the package id and catalog from a package uri
    /// </summary>
    /// <param name="packageUri">Input package uri</param>
    /// <returns>Package id and catalog, or null if the URI protocol is inaccurate</returns>
    public Task<WinGetProtocolParserResult> ParseAsync(Uri packageUri);
}
