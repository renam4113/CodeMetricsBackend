using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeMetrics.Migrations
{
    /// <inheritdoc />
    public partial class FixMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommitStats_Commits_CommitHash",
                table: "CommitStats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CommitStats",
                table: "CommitStats");

            migrationBuilder.DropColumn(
                name: "id",
                table: "CommitStats");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Repositories",
                newName: "RepoName");

            migrationBuilder.RenameIndex(
                name: "IX_Repositories_Name",
                table: "Repositories",
                newName: "IX_Repositories_RepoName");

            migrationBuilder.AlterColumn<string>(
                name: "CommitHash",
                table: "CommitStats",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommitStats",
                table: "CommitStats",
                column: "CommitHash");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchName = table.Column<string>(type: "text", nullable: false),
                    repoName = table.Column<string>(type: "text", nullable: false),
                    lastCommitHash = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchName);
                    table.ForeignKey(
                        name: "FK_Branches_Commits_lastCommitHash",
                        column: x => x.lastCommitHash,
                        principalTable: "Commits",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Branches_Repositories_repoName",
                        column: x => x.repoName,
                        principalTable: "Repositories",
                        principalColumn: "RepoName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserEmail);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commits_authorEmail",
                table: "Commits",
                column: "authorEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_committerEmail",
                table: "Commits",
                column: "committerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_lastCommitHash",
                table: "Branches",
                column: "lastCommitHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_repoName",
                table: "Branches",
                column: "repoName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserEmail",
                table: "Users",
                column: "UserEmail",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Commits_CommitStats_Hash",
                table: "Commits",
                column: "Hash",
                principalTable: "CommitStats",
                principalColumn: "CommitHash",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Commits_Users_authorEmail",
                table: "Commits",
                column: "authorEmail",
                principalTable: "Users",
                principalColumn: "UserEmail",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commits_Users_committerEmail",
                table: "Commits",
                column: "committerEmail",
                principalTable: "Users",
                principalColumn: "UserEmail",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commits_CommitStats_Hash",
                table: "Commits");

            migrationBuilder.DropForeignKey(
                name: "FK_Commits_Users_authorEmail",
                table: "Commits");

            migrationBuilder.DropForeignKey(
                name: "FK_Commits_Users_committerEmail",
                table: "Commits");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CommitStats",
                table: "CommitStats");

            migrationBuilder.DropIndex(
                name: "IX_Commits_authorEmail",
                table: "Commits");

            migrationBuilder.DropIndex(
                name: "IX_Commits_committerEmail",
                table: "Commits");

            migrationBuilder.RenameColumn(
                name: "RepoName",
                table: "Repositories",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Repositories_RepoName",
                table: "Repositories",
                newName: "IX_Repositories_Name");

            migrationBuilder.AlterColumn<string>(
                name: "CommitHash",
                table: "CommitStats",
                type: "varchar(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "CommitStats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommitStats",
                table: "CommitStats",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommitStats_Commits_CommitHash",
                table: "CommitStats",
                column: "CommitHash",
                principalTable: "Commits",
                principalColumn: "Hash",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
