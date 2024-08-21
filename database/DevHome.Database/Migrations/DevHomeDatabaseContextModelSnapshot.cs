// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

                b.Property<string>("RepositoryClonePath")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(string.Empty);

                b.Property<string>("RepositoryName")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(string.Empty);

                b.HasKey("RepositoryId");

                b.HasIndex("RepositoryName", "RepositoryClonePath")
                    .IsUnique();

                b.ToTable("Repositories");
            });

        modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", b =>
            {
                b.Property<int>("RepositoryMetadataId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<bool>("IsHiddenFromPage")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasDefaultValue(false);

                b.Property<int>("RepositoryId")
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("UtcDateHidden")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("TEXT")
                    .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

                b.HasKey("RepositoryMetadataId");

                b.HasIndex("RepositoryId")
                    .IsUnique();

                b.ToTable("RepositoryMetadata");
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
