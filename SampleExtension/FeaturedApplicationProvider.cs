// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace SampleExtension;

/// <summary>
/// Sample implementation of IFeaturedApplicationProvider based on a static list of featured applications.
/// </summary>
public class FeaturedApplicationProvider : IFeaturedApplicationProvider
{
    public IAsyncOperation<GetFeaturedApplicationGroupResult> GetFeaturedApplicationGroupsAsync()
    {
        return Task.FromResult(new GetFeaturedApplicationGroupResult(new FeaturedApplicationGroup())).AsAsyncOperation();
    }

    /// <summary>
    /// Sample implementation of IFeaturedApplicationGroup.
    /// </summary>
    private class FeaturedApplicationGroup : IFeaturedApplicationGroup
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

        public string GetDescription(string preferredLocale) => $"Sample {nameof(FeaturedApplicationGroup)} description";

        public string GetTitle(string preferredLocale) => $"Sample {nameof(FeaturedApplicationGroup)} title";
    }
}
