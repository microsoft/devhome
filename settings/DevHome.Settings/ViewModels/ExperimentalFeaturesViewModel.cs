// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public partial class ExperimentalFeature : ObservableObject
{
    public string Id { get; init; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public ExperimentalFeature(string id)
    {
        Id = id;
        IsEnabled = GetIsEnabled();
    }

    public string Name
    {
        get
        {
            var stringResource = new StringResource("DevHome.Settings/Resources");
            return stringResource.GetLocalized(Id + "_Name");
        }
    }

    public string Description
    {
        get
        {
            var stringResource = new StringResource("DevHome.Settings/Resources");
            return stringResource.GetLocalized(Id + "_Description");
        }
    }

    [ObservableProperty]
    private bool isEnabled;

    public bool GetIsEnabled()
    {
        return LocalSettingsService!.ReadSettingAsync<bool>($"ExperimentalFeature_{Id}").Result;
    }

    partial void OnIsEnabledChanging(bool value)
    {
        LocalSettingsService!.SaveSettingAsync($"ExperimentalFeature_{Id}", value).Wait();
    }
}

public class ExperimentalFeaturesViewModel : ObservableObject
{
    public ExperimentalFeature[] InternalFeatureDefinitions
    {
        get;
    }

    private ILocalSettingsService _localSettingsService;

    public ObservableCollection<ExperimentalFeature> Features { get; } = new ();

    public ExperimentalFeaturesViewModel(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        ExperimentalFeature.LocalSettingsService = localSettingsService;
        InternalFeatureDefinitions = new[]
        {
            new ExperimentalFeature("ExperimentalFeature_1"),
            new ExperimentalFeature("ExperimentalFeature_2"),
        };

        const string prefix = "ExperimentalFeature_";
        var features = _localSettingsService.EnumerateSettings().Result
            .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(s => new ExperimentalFeature(s.Substring(prefix.Length)));
        var joined = features.Concat(InternalFeatureDefinitions
            .Where(f => !features.Any(f2 => f2.Id == f.Id)));

        var sorted = joined.ToImmutableSortedSet(Comparer<ExperimentalFeature>.Create((f1, f2) => string.Compare(f1.Id, f2.Id, StringComparison.OrdinalIgnoreCase)));
        foreach (var feature in sorted)
        {
            Features.Add(feature);
        }
    }
}
