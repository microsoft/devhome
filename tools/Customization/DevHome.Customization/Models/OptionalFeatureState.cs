// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;

namespace DevHome.Customization.Models;

public partial class OptionalFeatureState : ObservableObject
{
    private WindowsOptionalFeature _feature;

    public WindowsOptionalFeature Feature
    {
        get => _feature;

        set
        {
            var wasChanged = HasChanged;
            _feature = value;
            IsEnabled = value.IsEnabled;

            if (wasChanged)
            {
                OnPropertyChanged(nameof(HasChanged));
            }
        }
    }

    private readonly bool _isModifiable;

    public bool IsModifiable => _isModifiable && !_isCommandRunning;

    private bool _isCommandRunning;

    public bool IsCommandRunning
    {
        get => _isCommandRunning;
        set
        {
            if (SetProperty(ref _isCommandRunning, value))
            {
                OnPropertyChanged(nameof(IsModifiable));
            }
        }
    }

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

    public OptionalFeatureState(WindowsOptionalFeature feature, bool modifiable, IAsyncRelayCommand applyChangesCommand)
    {
        _feature = feature;
        _isEnabled = feature.IsEnabled;
        _isModifiable = modifiable;

        // Ensure that when a command is running, the feature state is not modifiable.
        if (modifiable)
        {
            applyChangesCommand.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IAsyncRelayCommand.IsRunning))
                {
                    IsCommandRunning = applyChangesCommand.IsRunning;
                }
            };
        }
    }
}
