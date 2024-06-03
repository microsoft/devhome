// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WinRT;

namespace DevHome.SetupFlow.Common.Extensions;

public static class WinRTObjectExtensions
{
    public static TOutput GetValueOrDefault<TProjectedClass, TOutput>(
        this TProjectedClass projectedClassInstance,
        Func<TProjectedClass, TOutput> getValueFunc,
        TOutput defaultValue)
        where TProjectedClass : IWinRTObject
    {
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
