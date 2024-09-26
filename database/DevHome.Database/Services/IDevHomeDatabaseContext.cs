// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Windows.ApplicationModel.Appointments.AppointmentsProvider;

namespace DevHome.Database.Services;

public interface IDevHomeDatabaseContext : IDisposable
{
    DbSet<Repository> Repositories { get; set; }

    uint SchemaVersion { get; }

    DatabaseFacade Database { get; }

    EntityEntry Add(Repository repository);

    int SaveChanges();
}
