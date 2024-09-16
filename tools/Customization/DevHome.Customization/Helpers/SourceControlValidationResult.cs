// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Customization.Helpers;

public class SourceControlValidationResult
{
    public ResultType Result { get; private set; } = ResultType.Unknown;

    public ErrorType Error { get; private set; } = ErrorType.Unknown;

    public Exception? Exception
    {
        get; set;
    }

    public string? DisplayMessage
    {
        get; set;
    }

    public string? DiagnosticText
    {
        get; set;
    }

    public SourceControlValidationResult()
    {
        Result = ResultType.Success;
        Error = ErrorType.None;
    }

    public SourceControlValidationResult(ResultType result, ErrorType error, Exception? exception, string? displayMessage, string? diagnosticText)
    {
        Result = result;
        Error = error;
        Exception = exception;
        DisplayMessage = displayMessage;
        DiagnosticText = diagnosticText;
    }
}
