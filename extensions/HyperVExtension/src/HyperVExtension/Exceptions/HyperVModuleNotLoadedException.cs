// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class HyperVModuleNotLoadedException : HyperVManagerException
{
    public HyperVModuleNotLoadedException(string message)
        : base(message)
    {
    }
}
