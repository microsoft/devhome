// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.SetupFlow.RepoConfig.Models;

/// <summary>
/// Local implementation of IRepository from the SDK.  Used for ISetupTask.Execute
/// </summary>
internal class Repository : Microsoft.Windows.DevHome.SDK.IRepository
{
    private readonly string _displayName;

    private readonly string _cloneUrl;

    public Repository(string displayName, string cloneUrl)
    {
        _displayName = displayName;
        _cloneUrl = cloneUrl;
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination, IDeveloperId forPrivateRepos)
    {
        return Task.Run(() =>
        {
            var cloneOptions = new LibGit2Sharp.CloneOptions();
            cloneOptions.Checkout = true;

            if (forPrivateRepos != null)
            {
                cloneOptions.CredentialsProvider = (url, user, credentials) => new UsernamePasswordCredentials
                {
                    Username = forPrivateRepos.LoginId(),
                    Password = string.Empty,
                };
            }

            try
            {
                LibGit2Sharp.Repository.Clone(_cloneUrl, cloneDestination, cloneOptions);
            }
            catch (Exception)
            {
                // todo: handle the exception and log it
                throw;
            }
        }).AsAsyncAction();
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination)
    {
        return CloneRepositoryAsync(cloneDestination, null);
    }

    public string DisplayName() => _displayName;

    public string GetOwningAccountName() => throw new NotImplementedException();
}
