// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Customization.Helpers;

public class SourceControlValidationResult
{
    public ResultType Result { get; private set; } = ResultType.Unknown;

    public ErrorType Error { get; private set; } = ErrorType.Unknown;

    public Exception? Exception
    {
        get; private set;
    }

    public SourceControlValidationResult()
    {
        Result = ResultType.Success;
        Error = ErrorType.None;
    }

    public SourceControlValidationResult(ResultType result, ErrorType error, Exception? exception)
    {
        Result = result;
        Error = error;
        Exception = exception;
    }
}
