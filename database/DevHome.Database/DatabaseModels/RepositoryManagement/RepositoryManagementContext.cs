// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

public class RepositoryManagementContext : DbContext
{
    public DbSet<Repository> Repositories { get; set; }

    public DbSet<RepositoryCommit> RepositoryCommits { get; set; }

    public DbSet<RepositoryManagement> RepositoryManagements { get; set; }

    public string DbPath { get; }

    public RepositoryManagementContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "DevHome.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}").LogTo(x => Debug.WriteLine(x)).EnableSensitiveDataLogging();
}
