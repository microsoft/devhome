// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace SampleExtension;

/// <summary>
/// Sample implementation of IFeaturedApplicationProvider based on a static list of featured applications.
/// </summary>
public class FeaturedApplicationsProvider : IFeaturedApplicationsProvider
{
    public IAsyncOperation<GetFeaturedApplicationsGroupsResult> GetFeaturedApplicationsGroupsAsync()
    {
        return Task.FromResult(new GetFeaturedApplicationsGroupsResult(new List<IFeaturedApplicationsGroup>()
        {
            new FeaturedApplicationsGroup(),
        })).AsAsyncOperation();
    }

    /// <summary>
    /// Sample implementation of IFeaturedApplicationsGroup.
    /// </summary>
    private sealed class FeaturedApplicationsGroup : IFeaturedApplicationsGroup
    {
        public GetFeaturedApplicationsResult GetApplications()
        {
            // Sample list of featured applications
            return new GetFeaturedApplicationsResult(new List<string>()
            {
                "x-ms-winget://winget/Microsoft.VisualStudio.2022.Community",
                "x-ms-winget://winget/Microsoft.VisualStudioCode",
                "x-ms-winget://winget/Microsoft.PowerShell",
                "x-ms-winget://winget/Git.Git",
            });
        }

        public string GetDescription(string preferredLocale) => $"Sample {nameof(FeaturedApplicationsGroup)} description";

        public string GetTitle(string preferredLocale) => $"Sample {nameof(FeaturedApplicationsGroup)} title";
    }
}
