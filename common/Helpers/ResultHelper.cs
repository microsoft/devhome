// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.Common.ResultHelper;

public class HresultException : Exception
{
    public HresultException(int errorCode)
    {
        HResult = errorCode;
    }
}

public static class Result
{
    /// <summary>
    /// Throw an exception if <paramref name="hresult"/> is an error.
    /// </summary>
    /// <param name="hresult">HRESULT to check.</param>
    public static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0)
        {
            throw new HresultException(hresult);
        }
    }
}
