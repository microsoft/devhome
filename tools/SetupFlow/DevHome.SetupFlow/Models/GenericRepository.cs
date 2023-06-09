// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Win32.Storage.FileSystem;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace DevHome.SetupFlow.Models;
internal class GenericRepository : Microsoft.Windows.DevHome.SDK.IRepository
{
    private readonly string _displayName;

    public string DisplayName => _displayName;

    public bool IsPrivate => false;

    public DateTimeOffset LastUpdated => DateTime.UtcNow;

    public string OwningAccountName => "Unknown";

    private readonly Uri _cloneUri;

    public GenericRepository(Uri cloneUri)
    {
        _displayName = cloneUri.Segments[cloneUri.Segments.Length - 1].ToString().Replace("/", string.Empty);
        _cloneUri = cloneUri;
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
                    Log.Logger?.ReportError("GenericRepository", "Could not clone all sub modules", recurseException);
                    throw;
                }
                catch (UserCancelledException userCancelledException)
                {
                    Log.Logger?.ReportError("GenericRepository", "The user stoped the clone operation", userCancelledException);
                    throw;
                }
                catch (NameConflictException nameConflictException)
                {
                    Log.Logger?.ReportError("GenericRepository", string.Empty, nameConflictException);
                    throw;
                }
                catch (Exception e)
                {
                    Log.Logger?.ReportError("GenericRepository", "Could not clone the repository", e);
                    throw;
                }
            }
        }).AsAsyncAction();
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination) => throw new NotImplementedException();
}
