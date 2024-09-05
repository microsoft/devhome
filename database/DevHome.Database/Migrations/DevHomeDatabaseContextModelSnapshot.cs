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

                b.Property<bool>("IsHidden")
                    .HasColumnType("INTEGER");

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
#pragma warning restore 612, 618
    }
}
