// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupTargetReviewViewModel : ReviewTabViewModelBase
{
    private const string HyperVExtensionProviderName = "Microsoft.HyperV";

    private readonly HashSet<string> _osPropertyNameValues = new()
    {
        "osname",
        "os",
        "operatingSystem",
    };

    private readonly ISetupFlowStringResource _stringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    public override bool HasItems => _computeSystemManager.ComputeSystemSetupItem != null;

    public ComputeSystemReviewItem ComputeSystemSetupItem => _computeSystemManager.ComputeSystemSetupItem;

    public IEnumerable<ComputeSystemPropertyCache> ComputeSystemProperties { get; private set; }

    [ObservableProperty]
    private string _infoBarMessage;

    public string InfoBarTitle => _stringResource.GetLocalized(StringResourceKey.SetupTargetReviewPageDefaultInfoBarTitle);

    [ObservableProperty]
    private string _osName;

    public SetupTargetReviewViewModel(ISetupFlowStringResource stringResource, IComputeSystemManager computeSystemManager)
    {
        _stringResource = stringResource;
        TabTitle = stringResource.GetLocalized(StringResourceKey.SetupTargetReviewTabTitle);
        _computeSystemManager = computeSystemManager;
    }

    public async Task LoadViewModelContentAsync()
    {
        if (HasItems)
        {
            var isHyperVExtension = string.Equals(ComputeSystemSetupItem.AssociatedProvider.Id, HyperVExtensionProviderName, System.StringComparison.Ordinal);
            InfoBarMessage = isHyperVExtension
                ? _stringResource.GetLocalized(StringResourceKey.SetupTargetReviewPageHyperVInfoBarMessage)
                : _stringResource.GetLocalized(StringResourceKey.SetupTargetReviewPageDefaultInfoBarMessage);
        }

        await SetOsNameAsync();
    }

    private async Task SetOsNameAsync()
    {
        var computeSystem = _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup;
        ComputeSystemProperties = await computeSystem?.GetComputeSystemPropertiesAsync(string.Empty);
        OsName = null;

        if (ComputeSystemProperties == null)
        {
            return;
        }

        var osName = string.Empty;
        foreach (var property in ComputeSystemProperties)
        {
            var lowerCasePropertyName = property.Name.ToLowerInvariant();
            if (_osPropertyNameValues.Contains(lowerCasePropertyName))
            {
                osName = property.Value as string;
                break;
            }
        }

        if (!string.IsNullOrEmpty(osName))
        {
            OsName = osName;
        }
    }
}
