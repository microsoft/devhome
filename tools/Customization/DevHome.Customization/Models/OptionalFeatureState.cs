// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Helpers;

namespace DevHome.Customization.Models;

public partial class OptionalFeatureState : ObservableObject
{
    public WindowsOptionalFeature Feature { get; }

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

    public OptionalFeatureState(WindowsOptionalFeature feature)
    {
        Feature = feature;
        IsEnabled = feature.IsEnabled;
    }
}
