// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class HyperVAdminGroupException : HyperVManagerException
{
    public HyperVAdminGroupException(string message)
        : base(message)
    {
    }
}
