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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var repositoryEntity = modelBuilder.Entity<Repository>();
        if (repositoryEntity != null)
        {
            repositoryEntity.Property(x => x.RepositoryClonePath).HasDefaultValue(string.Empty);
            repositoryEntity.Property(x => x.RepositoryName).HasDefaultValue(string.Empty);
        }

        var repositoryMetadataEntity = modelBuilder.Entity<RepositoryMetadata>();
        if (repositoryMetadataEntity != null)
        {
            repositoryMetadataEntity.Property(x => x.IsHiddenFromPage).HasDefaultValue(false);
            repositoryMetadataEntity.Property(x => x.UtcDateHidden).HasDefaultValue(DateTime.MinValue);
        }
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}
