// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
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
        // TODO: make the default path Application data if an MSIX.
        // Otherwise use a temp location.  Location to be determined later.
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "DevHome.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Use ServiceExtensions as an example to set up individual
        // models using fluent API.  Currently, not needed, but will as this method
        // will expand as more entities are added.
        // If that is too much work these definitions can be placed inside the C# class.
        var repositoryEntity = modelBuilder.Entity<Repository>();
        if (repositoryEntity != null)
        {
            repositoryEntity.Property(x => x.RepositoryClonePath).HasDefaultValue(string.Empty).IsRequired(true);
            repositoryEntity.Property(x => x.RepositoryName).HasDefaultValue(string.Empty).IsRequired(true);
            repositoryEntity.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
            repositoryEntity.Property(x => x.UpdatedUTCDate).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            repositoryEntity.ToTable("Repository");
        }

        var repositoryMetadataEntity = modelBuilder.Entity<RepositoryMetadata>();
        if (repositoryMetadataEntity != null)
        {
            repositoryMetadataEntity.Property(x => x.IsHiddenFromPage).HasDefaultValue(false).IsRequired(true);
            repositoryMetadataEntity.Property(x => x.UtcDateHidden).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)).IsRequired(true);
            repositoryMetadataEntity.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
            repositoryMetadataEntity.Property(x => x.UpdatedUTCDate).HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            repositoryMetadataEntity.ToTable("RepositoryMetadata");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}
