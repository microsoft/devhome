// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.ViewModels;

public class ConfigureTaskViewModel
{
    private readonly ConfigureTask _task;

    public ConfigureTaskViewModel(ConfigureTask task)
    {
        _task = task;
    }
}
