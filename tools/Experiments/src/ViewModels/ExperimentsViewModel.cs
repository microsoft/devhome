// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Services;

namespace DevHome.Experiments.ViewModels;

public class TestExperimentViewModel : ObservableObject
{
    public TestExperimentViewModel()
    {
    }
}

public class ExperimentalFeature
{
    public string Id
    {
        get; set;
    }
    public bool Enabled
    {
        get; set;
    }
}


public class ExperimentsViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IApp _app;

    public static IList<ExperimentalFeature> ExperimentalFeatures
    {
        get;
    } = new List<ExperimentalFeature>();
    public ExperimentsViewModel(IApp app, ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        _app = app;
        foreach (var group in _app..NavConfig.NavMenu.Groups)
        {
            foreach (var tool in group.Tools)

    }
    }
