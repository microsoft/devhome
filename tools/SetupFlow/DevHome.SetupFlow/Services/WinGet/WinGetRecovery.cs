// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Exceptions;

namespace DevHome.SetupFlow.Services.WinGet;

public class WinGetRecovery : IWinGetRecovery
{
    // Recovery configuration
    private const int MaxAttempts = 3;
    private const int DelayMs = 1_000;

    // RPC error codes to recover from
    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private readonly IWinGetCatalogConnector _catalogConnector;

    public WinGetRecovery(IWinGetCatalogConnector catalogConnector)
    {
        _catalogConnector = catalogConnector;
    }

    public async Task<T> DoWithRecovery<T>(Func<Task<T>> actionFunc)
    {
        // Run action in a background thread to avoid blocking the UI thread
        // Async methods are blocking in WinGet: https://github.com/microsoft/winget-cli/issues/3205
        return await Task.Run(async () =>
        {
            var attempt = 0;
            while (++attempt <= MaxAttempts)
            {
                try
                {
                    return await actionFunc();
                }
                catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
                {
                    Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to operate on out-of-proc object with error code: 0x{e.HResult:x}");
                    if (attempt < MaxAttempts)
                    {
                        // Retry with exponential backoff
                        var backoffMs = DelayMs * (int)Math.Pow(2, attempt);
                        Log.Logger?.ReportError(Log.Component.AppManagement, $"Attempting to recover attempt number {attempt} in {backoffMs} ms");

                        // Wait for the backoff period
                        await Task.Delay(TimeSpan.FromMilliseconds(backoffMs));

                        // Recover catalogs
                        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Starting recovery ...");
                        await _catalogConnector.RecoverDisconnectedCatalogsAsync();
                        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Recovery complete");
                    }
                }
            }

            Log.Logger?.ReportError(Log.Component.AppManagement, $"Unable to recover windows package manager after {MaxAttempts} attempts");
            throw new WindowsPackageManagerRecoveryException();
        });
    }
}
