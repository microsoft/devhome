// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using static DevHome.Common.Helpers.WindowsOptionalFeatures;

namespace DevHome.Common.Models;

public partial class WindowsOptionalFeatureState : ObservableObject
{
    public FeatureInfo Feature { get; }

    [ObservableProperty]
    private bool _isModifiable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanged))]
    private bool _isEnabled;

    public bool HasChanged => IsEnabled != Feature.IsEnabled;

    public WindowsOptionalFeatureState(FeatureInfo feature, bool modifiable)
    {
        Feature = feature;
        IsEnabled = feature.IsEnabled;
        IsModifiable = modifiable;
    }
}
