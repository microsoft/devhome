// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public class ExperimentalFeaturesViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;

    public ObservableCollection<ExperimentalFeature> Features { get; } = new();

    public ExperimentalFeaturesViewModel(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        ExperimentalFeature.LocalSettingsService = localSettingsService;
    }
}
