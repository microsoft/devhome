// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHome.Database.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    private static readonly string[] _columns = new[] { "RepositoryName", "RepositoryClonePath" };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Repository",
            columns: table => new
            {
                RepositoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RepositoryName = table.Column<string>(type: "TEXT", nullable: false, defaultValue: string.Empty),
                RepositoryClonePath = table.Column<string>(type: "TEXT", nullable: false, defaultValue: string.Empty),
                IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                ConfigurationFileLocation = table.Column<string>(type: "TEXT", nullable: true, defaultValue: string.Empty),
                RepositoryUri = table.Column<string>(type: "TEXT", nullable: true, defaultValue: string.Empty),
<<<<<<<< HEAD:database/DevHome.Database/Migrations/20240911232143_InitialMigration.cs
                SourceControlClassId = table.Column<Guid>(type: "TEXT", nullable: true, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
========
>>>>>>>> main:database/DevHome.Database/Migrations/20240919221355_InitialMigration.cs
                CreatedUTCDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime()"),
                UpdatedUTCDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Repository", x => x.RepositoryId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Repository_RepositoryName_RepositoryClonePath",
            table: "Repository",
            columns: _columns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Repository");
    }
}
