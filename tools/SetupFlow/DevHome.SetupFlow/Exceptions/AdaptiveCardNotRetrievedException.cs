// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.Exceptions;

public class AdaptiveCardNotRetrievedException : Exception
{
    public AdaptiveCardNotRetrievedException(string message)
        : base(message)
    {
    }
}
