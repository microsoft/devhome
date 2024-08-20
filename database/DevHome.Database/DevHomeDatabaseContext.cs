// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database.DatabaseModels.RepositoryManagement;
using Microsoft.EntityFrameworkCore;

namespace DevHome.Database;

public class DevHomeDatabaseContext : DbContext
{
    public DbSet<Repository> Repositories { get; set; }

    public DbSet<RepositoryMetadata> RepositoryMetadata { get; set; }

    public string DbPath { get; }

    public DevHomeDatabaseContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "DevHome.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}
