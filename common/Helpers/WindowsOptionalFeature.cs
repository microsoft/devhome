// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Helpers;

public class WindowsOptionalFeature
{
    public string FeatureName { get; set; }

    public string DisplayName { get; set; }

    public string Description { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsAvailable { get; set; }

    public FeatureAvailabilityKind AvailabilityKind { get; set; }

    public WindowsOptionalFeature(string featureName, string displayName, string description, FeatureAvailabilityKind availabilityKind)
    {
        FeatureName = featureName;
        DisplayName = displayName;
        Description = description;
        AvailabilityKind = availabilityKind;
        IsEnabled = AvailabilityKind == FeatureAvailabilityKind.Enabled;
        IsAvailable = (AvailabilityKind == FeatureAvailabilityKind.Enabled) || (AvailabilityKind == FeatureAvailabilityKind.Disabled);
    }
}
