// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using WinRT;

namespace DevHome.SetupFlow.Extensions;
public static class WinRTObjectExtensions
{
    public static TOutput GetValueOrDefault<TProjectedClass, TOutput>(
        this TProjectedClass projectedClassInstance,
        Func<TProjectedClass, TOutput> getValueFunc,
        TOutput defaultValue)
        where TProjectedClass : IWinRTObject
    {
        // TODO Use API contract version to check if member is available
        // Modify the signature to take the current and min version
        try
        {
            return getValueFunc(projectedClassInstance);
        }
        catch
        {
            return defaultValue;
        }
    }
}
