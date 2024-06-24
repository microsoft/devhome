// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.UnitTest.Mocks;

public enum HyperVState
{
    Running,
    Off,
    Saved,
    Paused,
}

/// <summary>
/// Class used to mock PowerShell objects where a specific property is used
/// to identify output for a calling function. Add new properities to this class
/// or derive from this class should you need to mock output for a function that
/// looks at the properites of a returned PSCustomObject.
/// </summary>
public class PSCustomObjectMock
{
    public string Name { get; set; } = string.Empty;

    public string StandardName { get; set; } = string.Empty;

    public Enum? State { get; set; }

    public Guid Id { get; set; }

    public Guid ParentCheckpointId { get; set; }

    public string ParentCheckpointName { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public uint LogicalProcessorCount { get; set; }

    public string VirtualHardDiskPath { get; set; } = string.Empty;

    public string VirtualMachinePath { get; set; } = string.Empty;

    public long MemoryMaximum { get; set; }

    public long MemoryMinimum { get; set; }
}
