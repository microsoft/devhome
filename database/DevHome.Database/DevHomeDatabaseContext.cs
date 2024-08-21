// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database.DatabaseModels.RepositoryManagement;
using Microsoft.EntityFrameworkCore;

namespace DevHome.Database;

public class DevHomeDatabaseContext : DbContext
{
    public DbSet<Repository> Repositories { get; set; }

    public DbSet<RepositoryMetadata> RepositoryMetadatas { get; set; }

    public string DbPath { get; }

    public DevHomeDatabaseContext()
    {
        // Not the final path.  It will change before going into main.
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "DevHome.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // If I have time, this should be split like service extensions.
        // As more entities are added, the longer this method will get.
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}
