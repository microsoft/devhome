// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;

namespace DevHome.Common.Models;

public partial class OptionalFeatureState : ObservableObject
{
    public WindowsOptionalFeature Feature { get; }

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

    public OptionalFeatureState(WindowsOptionalFeature feature, bool modifiable)
    {
        Feature = feature;
        IsEnabled = feature.IsEnabled;
        IsModifiable = modifiable;
    }
}
