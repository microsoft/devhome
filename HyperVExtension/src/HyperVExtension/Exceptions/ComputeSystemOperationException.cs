// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.Exceptions;

public class ComputeSystemOperationException : Exception
{
    // This is used in times when the Hyper-V manager failed to perform an operation and did not receive an error from PowerShell.
    // This shouldn't happen, but in case it does, check the state of the virtual machine when the operation was requested
    // for debugging clues.
    private const string ErrorMessage = "{0} operation failed but no PowerShell error was received. Check the state of the virtual machine.";

    public ComputeSystemOperationException(ComputeSystemOperations operation)
        : base(string.Format(CultureInfo.InvariantCulture, ErrorMessage, operation))
    {
    }

    public ComputeSystemOperationException(string message)
        : base(message)
    {
    }

    public ComputeSystemOperationException(string? message, Exception? innerException)
    : base(message, innerException)
    {
    }
}
