// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Helpers;

public class WindowsOptionalFeature
{
    public string DisplayName { get; set; }

    public string Description { get; set; }

    public bool IsEnabled { get; set; }

    public FeatureAvailabilityKind AvailabilityKind { get; set; }

    public WindowsOptionalFeature(string displayName, string description, FeatureAvailabilityKind availabilityKind)
    {
        DisplayName = displayName;
        Description = description;
        AvailabilityKind = availabilityKind;
        IsEnabled = AvailabilityKind == FeatureAvailabilityKind.Enabled;
    }
}
