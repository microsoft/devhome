// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;
public partial class AddViaUrlViewModel : ObservableObject
{
    [ObservableProperty]
    private string _enteredUri;

    [ObservableProperty]
    private string _uriError;

    [ObservableProperty]
    private bool _shouldShowUriError;

    private readonly ISetupFlowStringResource _stringResource;

    public AddViaUrlViewModel(ISetupFlowStringResource stringResource)
    {
        _stringResource = stringResource;
    }

    public bool ValidateUri()
    {
        // Check if Url field is empty
        if (string.IsNullOrEmpty(EnteredUri))
        {
            return false;
        }

        if (!Uri.TryCreate(EnteredUri, UriKind.RelativeOrAbsolute, out _))
        {
            UriError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
            ShouldShowUriError = true;
            return false;
        }

        var sshMatch = Regex.Match(EnteredUri, "^.*@.*:.*\\/.*");

        if (sshMatch.Success)
        {
            UriError = _stringResource.GetLocalized(StringResourceKey.SSHConnectionStringNotAllowed);
            ShouldShowUriError = true;
            return false;
        }

        ShouldShowUriError = false;
        return true;
    }

    /// <summary>
    /// Adds a repository from the URL page. Steps to determine what repoProvider to use.
    /// 1. All providers are asked "Can you parse this into a URL you understand."  If yes, that provider to clone the repo.
    /// 2. If no providers can parse the URL a fall back "GitProvider" is used that uses libgit2sharp to clone the repo.
    /// ShouldShowUrlError is set here.
    /// </summary>
    /// <remarks>
    /// If ShouldShowUrlError == Visible the repo is not added to the list of repos to clone.
    /// </remarks>
    /// <param name="cloneLocation">The location to clone the repo to</param>
    public void AddRepositoryViaUri(RepositoryProviders providers, string cloneLocation, List<CloningInformation> previouslySelectedRepos, List<CloningInformation> everythingToClone)
    {
        // If the url isn't valid don't bother finding a provider.
        Uri parsedUri;
        if (!Uri.TryCreate(EnteredUri, UriKind.RelativeOrAbsolute, out parsedUri))
        {
            UriError = _stringResource.GetLocalized(StringResourceKey.UrlValidationBadUrl);
            ShouldShowUriError = true;
            return;
        }

        // If user entered a relative Uri put it into a UriBuilder to turn it into an
        // absolute Uri.  UriBuilder prepends the https scheme
        if (!parsedUri.IsAbsoluteUri)
        {
            var uriBuilder = new UriBuilder(parsedUri.OriginalString);
            uriBuilder.Port = -1;
            parsedUri = uriBuilder.Uri;
        }

        // If the URL points to a private repo the URL tab has no way of knowing what account has access.
        // Keep owning account null to make github extension try all logged in accounts.
        (string, IRepository) providerNameAndRepo;

        try
        {
            providerNameAndRepo = providers.ParseRepositoryFromUri(parsedUri);
        }
        catch (Exception e)
        {
            // Github extension throws if the URL is parsed but the repo can't be found.
            // This can happen if
            // 1. Any logged in account does not have access
            // 2. The repo does not exist.
            UriError = _stringResource.GetLocalized(StringResourceKey.UrlValidationNotFound);
            ShouldShowUriError = true;
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, e.ToString());
            TelemetryFactory.Get<ITelemetry>().LogMeasure("RepoDialog_RepoNotFound_Event");
            return;
        }

        CloningInformation cloningInformation;
        if (providerNameAndRepo.Item2 != null)
        {
            // A provider parsed the URL and at least 1 logged in account has access to the repo.
            var repository = providerNameAndRepo.Item2;
            cloningInformation = new CloningInformation(repository);
            cloningInformation.ProviderName = providerNameAndRepo.Item1;
            cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
        }
        else
        {
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, "No providers could parse the Url.  Falling back to internal git provider");

            // No providers can parse the Url.
            // Fall back to a git Url.
            cloningInformation = new CloningInformation(new GenericRepository(parsedUri));
            cloningInformation.ProviderName = "git";
            cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
        }

        // User could paste in a url of an already added repo.  Check for that here.
        if (previouslySelectedRepos.Any(x => x.RepositoryToClone.OwningAccountName.Equals(cloningInformation.RepositoryToClone.OwningAccountName, StringComparison.OrdinalIgnoreCase)
            && x.RepositoryToClone.DisplayName.Equals(cloningInformation.RepositoryToClone.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            UriError = _stringResource.GetLocalized(StringResourceKey.UrlValidationRepoAlreadyAdded);
            ShouldShowUriError = true;
            Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Repository has already been added.");
            TelemetryFactory.Get<ITelemetry>().LogMeasure("RepoTool_RepoAlreadyAdded_Event");
            return;
        }

        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Adding repository to clone {cloningInformation.RepositoryId} to location '{cloneLocation}'");

        everythingToClone.Add(cloningInformation);
    }
}
