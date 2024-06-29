// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace DevHome.Services.WindowsPackageManager.Contracts;

internal interface IWinGetRecovery
{
    /// <summary>
    /// Run the provided action with recovery logic
    /// </summary>
    /// <typeparam name="T">Action return type</typeparam>
    /// <param name="actionFunc">Action to run</param>
    /// <returns>Action result</returns>
    public Task<T> DoWithRecoveryAsync<T>(Func<Task<T>> actionFunc);
}
