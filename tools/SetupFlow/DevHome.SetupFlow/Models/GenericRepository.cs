// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

internal sealed class GenericRepository : Microsoft.Windows.DevHome.SDK.IRepository
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GenericRepository));

    private readonly string _displayName;

    public string DisplayName => _displayName;

    public bool IsPrivate => false;

    public DateTimeOffset LastUpdated => DateTime.UtcNow;

    public string OwningAccountName => string.Empty;

    public Uri RepoUri => _cloneUri;

    private readonly Uri _cloneUri;

    public GenericRepository(Uri cloneUri)
    {
        _displayName = cloneUri.Segments[cloneUri.Segments.Length - 1].ToString().Replace("/", string.Empty);

        if (cloneUri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var locationOfHost = cloneUri.OriginalString.IndexOf("www.", StringComparison.OrdinalIgnoreCase);
            var originalStringWithoutHost = cloneUri.OriginalString.Remove(locationOfHost, 4);
            _cloneUri = new Uri(originalStringWithoutHost);
        }
        else
        {
            _cloneUri = cloneUri;
        }
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination, IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(cloneDestination))
            {
                var cloneOptions = new CloneOptions
                {
                    Checkout = true,
                };

                try
                {
                    LibGit2Sharp.Repository.Clone(_cloneUri.OriginalString, cloneDestination, cloneOptions);
                }
                catch (RecurseSubmodulesException recurseException)
                {
                    _log.Error(recurseException, "Could not clone all sub modules");
                    throw;
                }
                catch (UserCancelledException userCancelledException)
                {
                    _log.Error(userCancelledException, "The user stoped the clone operation");
                    throw;
                }
                catch (NameConflictException nameConflictException)
                {
                    _log.Error(nameConflictException, nameConflictException.ToString());
                    throw;
                }
                catch (Exception e)
                {
                    _log.Error(e, "Could not clone the repository");
                    throw;
                }
            }
        }).AsAsyncAction();
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination) => throw new NotImplementedException();
}
