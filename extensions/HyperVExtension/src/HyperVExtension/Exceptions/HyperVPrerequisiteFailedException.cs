// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class HyperVPrerequisiteFailedException : Exception
{
    public HyperVPrerequisiteFailedException(string message)
        : base(message)
    {
    }
}
