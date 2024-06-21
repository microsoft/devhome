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

    private bool _isEnabled;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                OnPropertyChanged(nameof(HasChanged));
            }
        }
    }

    public bool HasChanged => IsEnabled != Feature.IsEnabled;

    public WindowsOptionalFeatureState(FeatureInfo feature, bool modifiable)
    {
        Feature = feature;
        IsEnabled = feature.IsEnabled;
        IsModifiable = modifiable;
    }
}
