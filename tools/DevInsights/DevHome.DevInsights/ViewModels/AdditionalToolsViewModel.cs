// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.DevInsights.Helpers;
using Serilog;

namespace DevHome.DevInsights.ViewModels;

public partial class AdditionalToolsViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AdditionalToolsViewModel));

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AdditionalToolsViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(CommonHelper.GetLocalizedString("SettingsPageHeader"), typeof(SettingsPageViewModel).FullName!),
            new(CommonHelper.GetLocalizedString("SettingsAdditionalToolsHeader"), typeof(AdditionalToolsViewModel).FullName!),
        };
    }
}
