// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHome.Database.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Repositories",
            columns: table => new
            {
                RepositoryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RepositoryName = table.Column<string>(type: "TEXT", nullable: true),
                RepositoryClonePath = table.Column<string>(type: "TEXT", nullable: true),
                LocalBranchName = table.Column<string>(type: "TEXT", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Repositories", x => x.RepositoryId);
            });

        migrationBuilder.CreateTable(
            name: "RepositoryCommits",
            columns: table => new
            {
                RepositoryCommitId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CommitHash = table.Column<Guid>(type: "TEXT", nullable: false),
                CommitUri = table.Column<string>(type: "TEXT", nullable: true),
                Author = table.Column<string>(type: "TEXT", nullable: true),
                CommitDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                RepositoryId = table.Column<int>(type: "INTEGER", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RepositoryCommits", x => x.RepositoryCommitId);
                table.ForeignKey(
                    name: "FK_RepositoryCommits_Repositories_RepositoryId",
                    column: x => x.RepositoryId,
                    principalTable: "Repositories",
                    principalColumn: "RepositoryId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RepositoryManagements",
            columns: table => new
            {
                RepositoryManagementId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                IsHiddenFromPage = table.Column<bool>(type: "INTEGER", nullable: false),
                UtcDateHidden = table.Column<DateTime>(type: "TEXT", nullable: false),
                RepositoryId = table.Column<int>(type: "INTEGER", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RepositoryManagements", x => x.RepositoryManagementId);
                table.ForeignKey(
                    name: "FK_RepositoryManagements_Repositories_RepositoryId",
                    column: x => x.RepositoryId,
                    principalTable: "Repositories",
                    principalColumn: "RepositoryId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RepositoryCommits_RepositoryId",
            table: "RepositoryCommits",
            column: "RepositoryId");

        migrationBuilder.CreateIndex(
            name: "IX_RepositoryManagements_RepositoryId",
            table: "RepositoryManagements",
            column: "RepositoryId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RepositoryCommits");

        migrationBuilder.DropTable(
            name: "RepositoryManagements");

        migrationBuilder.DropTable(
            name: "Repositories");
    }
}
