// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models;

/// <summary>
/// Class that represents a checkpoint.
/// </summary>
public class Checkpoint
{
    public Guid ParentCheckpointId { get; set; }

    public string ParentCheckpointName { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<Checkpoint> ChildCheckpoints { get; set; } = new List<Checkpoint>();

    public Checkpoint(Guid parentCheckpointId, string parentCheckpointName, Guid checkpointId, string checkpointName)
    {
        ParentCheckpointId = parentCheckpointId;
        ParentCheckpointName = parentCheckpointName;
        Id = checkpointId;
        Name = checkpointName;
    }

    public Checkpoint()
    {
    }
}
