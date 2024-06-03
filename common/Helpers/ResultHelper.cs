// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Common.ResultHelper;

public static class ResultHelper
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

    public class HresultException : Exception
    {
        public HresultException(int errorCode)
        {
            HResult = errorCode;
        }
    }
}
