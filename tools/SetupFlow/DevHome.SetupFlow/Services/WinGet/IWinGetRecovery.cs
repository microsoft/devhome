// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services.WinGet;

public interface IWinGetRecovery
{
    /// <summary>
    /// Run the provided action with recovery logic
    /// </summary>
    /// <typeparam name="T">Action return type</typeparam>
    /// <param name="actionFunc">Action to run</param>
    /// <returns>Action result</returns>
    public Task<T> DoWithRecovery<T>(Func<Task<T>> actionFunc);
}
