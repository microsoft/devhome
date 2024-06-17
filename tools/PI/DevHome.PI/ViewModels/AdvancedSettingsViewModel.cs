// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.PI.Helpers;
using Serilog;

namespace DevHome.PI.ViewModels;

public partial class AdvancedSettingsViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AdvancedSettingsViewModel));

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AdvancedSettingsViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(CommonHelper.GetLocalizedString("SettingsPageHeader"), typeof(SettingsPageViewModel).FullName!),
            new(CommonHelper.GetLocalizedString("SettingsAdvancedSettingsHeader"), typeof(AdvancedSettingsViewModel).FullName!),
        };
    }
}
