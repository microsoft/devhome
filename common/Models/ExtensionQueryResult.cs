// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Models;
public class ExtensionQueryResult<TResult>
{
    public bool IsSuccessful { get; }

    public TResult? ResultData { get; }

    public Exception? Exception { get; }

    public ExtensionQueryResult(bool isSuccessful, TResult? resultData, Exception? exception)
    {
        IsSuccessful = isSuccessful;
        ResultData = resultData;
        Exception = exception;
    }
}
