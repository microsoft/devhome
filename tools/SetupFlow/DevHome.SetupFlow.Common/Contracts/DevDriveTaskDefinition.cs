// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class DevDriveTaskDefinition : ITaskDefinition
{
    public Guid TaskId
    {
        get; set;
    }

    public string VirtDiskPath
    {
        get; set;
    }

    public ulong SizeInBytes
    {
        get; set;
    }

    public char NewDriveLetter
    {
        get; set;
    }

    public string DriveLabel
    {
        get; set;
    }
}
