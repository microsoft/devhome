// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Class used to hold information related to a compute system that needs to be setup on the
/// machine configuration page.
/// </summary>
public class ComputeSystemReviewItem
{
    public ComputeSystem ComputeSystemToSetup { get; set; }

    public ComputeSystemProvider AssociatedProvider { get; set; }

    public ComputeSystemReviewItem(ComputeSystem computeSystemToSetup, ComputeSystemProvider associatedProvider)
    {
        ComputeSystemToSetup = computeSystemToSetup;
        AssociatedProvider = associatedProvider;
    }
}
