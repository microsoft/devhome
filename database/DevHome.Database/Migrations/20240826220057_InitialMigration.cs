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
                CreatedUTCDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime()"),
                UpdatedUTCDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Repository", x => x.RepositoryId);
            });

        migrationBuilder.CreateTable(
            name: "RepositoryMetadata",
            columns: table => new
            {
                RepositoryMetadataId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                IsHiddenFromPage = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                UtcDateHidden = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)),
                CreatedUTCDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime()"),
                UpdatedUTCDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)),
                RepositoryId = table.Column<int>(type: "INTEGER", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RepositoryMetadata", x => x.RepositoryMetadataId);
                table.ForeignKey(
                    name: "FK_RepositoryMetadata_Repository_RepositoryId",
                    column: x => x.RepositoryId,
                    principalTable: "Repository",
                    principalColumn: "RepositoryId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Repository_RepositoryName_RepositoryClonePath",
            table: "Repository",
            columns: _columns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RepositoryMetadata_RepositoryId",
            table: "RepositoryMetadata",
            column: "RepositoryId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RepositoryMetadata");

        migrationBuilder.DropTable(
            name: "Repository");
    }
}
