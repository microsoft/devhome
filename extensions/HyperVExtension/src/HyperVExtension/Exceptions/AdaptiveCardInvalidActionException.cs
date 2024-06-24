// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class AdaptiveCardInvalidActionException : Exception
{
    public AdaptiveCardInvalidActionException(string message)
        : base(message)
    {
    }
}
