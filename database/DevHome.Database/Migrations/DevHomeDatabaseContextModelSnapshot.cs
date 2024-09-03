// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace DevHome.Database.Migrations;

[DbContext(typeof(DevHomeDatabaseContext))]
public partial class DevHomeDatabaseContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

        modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.Repository", b =>
            {
                b.Property<int>("RepositoryId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime?>("CreatedUTCDate")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime()");

                b.Property<string>("RepositoryClonePath")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(string.Empty);

                b.Property<string>("RepositoryName")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(string.Empty);

                b.Property<DateTime?>("UpdatedUTCDate")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                b.HasKey("RepositoryId");

                b.HasIndex("RepositoryName", "RepositoryClonePath")
                    .IsUnique();

                b.ToTable("Repository", (string)null);
            });

        modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", b =>
            {
                b.Property<int>("RepositoryMetadataId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime?>("CreatedUTCDate")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime()");

                b.Property<bool>("IsHiddenFromPage")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasDefaultValue(false);

                b.Property<int>("RepositoryId")
                    .HasColumnType("INTEGER");

                b.Property<DateTime?>("UpdatedUTCDate")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                b.Property<DateTime>("UtcDateHidden")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                b.HasKey("RepositoryMetadataId");

                b.HasIndex("RepositoryId")
                    .IsUnique();

                b.ToTable("RepositoryMetadata", (string)null);
            });

        modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", b =>
            {
                b.HasOne("DevHome.Database.DatabaseModels.RepositoryManagement.Repository", "Repository")
                    .WithOne("RepositoryMetadata")
                    .HasForeignKey("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", "RepositoryId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Repository");
            });

        modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.Repository", b =>
            {
                b.Navigation("RepositoryMetadata");
            });
#pragma warning restore 612, 618
    }
}
