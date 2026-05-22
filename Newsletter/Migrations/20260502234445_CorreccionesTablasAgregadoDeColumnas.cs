using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Newsletter.Migrations
{
    /// <inheritdoc />
    public partial class CorreccionesTablasAgregadoDeColumnas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Games_ListGamesId",
                table: "GameGenre");

            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Genres_ListGenresId",
                table: "GameGenre");

            migrationBuilder.DropForeignKey(
                name: "FK_GameLibraries_AspNetUsers_UserId",
                table: "GameLibraries");

            migrationBuilder.DropTable(
                name: "GameGameLibrary");

            migrationBuilder.DropTable(
                name: "GamePlatform");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameLibraries",
                table: "GameLibraries");

            migrationBuilder.DropIndex(
                name: "IX_GameLibraries_UserId",
                table: "GameLibraries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre");

            migrationBuilder.DropIndex(
                name: "IX_GameGenre_ListGenresId",
                table: "GameGenre");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GameLibraries");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "GameLibraries",
                newName: "GameId");

            migrationBuilder.RenameColumn(
                name: "ListGenresId",
                table: "GameGenre",
                newName: "GenresId");

            migrationBuilder.RenameColumn(
                name: "ListGamesId",
                table: "GameGenre",
                newName: "gamesId");

            migrationBuilder.AddColumn<bool>(
                name: "State",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LibraryId",
                table: "GameLibraries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "State",
                table: "GameLibraries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "GameLibraryGameId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GameLibraryLibraryId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameLibraries",
                table: "GameLibraries",
                columns: new[] { "LibraryId", "GameId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre",
                columns: new[] { "GenresId", "gamesId" });

            migrationBuilder.CreateTable(
                name: "GamePlatforms",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<bool>(type: "bit", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlatforms", x => new { x.GameId, x.PlatformId });
                    table.ForeignKey(
                        name: "FK_GamePlatforms_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GamePlatforms_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Libraries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameLibraries_GameId",
                table: "GameLibraries",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameGenre_gamesId",
                table: "GameGenre",
                column: "gamesId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GameLibraryLibraryId_GameLibraryGameId",
                table: "AspNetUsers",
                columns: new[] { "GameLibraryLibraryId", "GameLibraryGameId" });

            migrationBuilder.CreateIndex(
                name: "IX_GamePlatforms_PlatformId",
                table: "GamePlatforms",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_UserId",
                table: "Libraries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GameLibraries_GameLibraryLibraryId_GameLibraryGameId",
                table: "AspNetUsers",
                columns: new[] { "GameLibraryLibraryId", "GameLibraryGameId" },
                principalTable: "GameLibraries",
                principalColumns: new[] { "LibraryId", "GameId" });

            migrationBuilder.AddForeignKey(
                name: "FK_GameGenre_Games_gamesId",
                table: "GameGenre",
                column: "gamesId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameGenre_Genres_GenresId",
                table: "GameGenre",
                column: "GenresId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameLibraries_Games_GameId",
                table: "GameLibraries",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameLibraries_Libraries_LibraryId",
                table: "GameLibraries",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GameLibraries_GameLibraryLibraryId_GameLibraryGameId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Games_gamesId",
                table: "GameGenre");

            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Genres_GenresId",
                table: "GameGenre");

            migrationBuilder.DropForeignKey(
                name: "FK_GameLibraries_Games_GameId",
                table: "GameLibraries");

            migrationBuilder.DropForeignKey(
                name: "FK_GameLibraries_Libraries_LibraryId",
                table: "GameLibraries");

            migrationBuilder.DropTable(
                name: "GamePlatforms");

            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameLibraries",
                table: "GameLibraries");

            migrationBuilder.DropIndex(
                name: "IX_GameLibraries_GameId",
                table: "GameLibraries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre");

            migrationBuilder.DropIndex(
                name: "IX_GameGenre_gamesId",
                table: "GameGenre");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GameLibraryLibraryId_GameLibraryGameId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "LibraryId",
                table: "GameLibraries");

            migrationBuilder.DropColumn(
                name: "State",
                table: "GameLibraries");

            migrationBuilder.DropColumn(
                name: "GameLibraryGameId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GameLibraryLibraryId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "GameId",
                table: "GameLibraries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "gamesId",
                table: "GameGenre",
                newName: "ListGamesId");

            migrationBuilder.RenameColumn(
                name: "GenresId",
                table: "GameGenre",
                newName: "ListGenresId");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GameLibraries",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameLibraries",
                table: "GameLibraries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre",
                columns: new[] { "ListGamesId", "ListGenresId" });

            migrationBuilder.CreateTable(
                name: "GameGameLibrary",
                columns: table => new
                {
                    GamesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListGameLibrariesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameGameLibrary", x => new { x.GamesId, x.ListGameLibrariesId });
                    table.ForeignKey(
                        name: "FK_GameGameLibrary_GameLibraries_ListGameLibrariesId",
                        column: x => x.ListGameLibrariesId,
                        principalTable: "GameLibraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameGameLibrary_Games_GamesId",
                        column: x => x.GamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GamePlatform",
                columns: table => new
                {
                    ListGamesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListPlatformsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlatform", x => new { x.ListGamesId, x.ListPlatformsId });
                    table.ForeignKey(
                        name: "FK_GamePlatform_Games_ListGamesId",
                        column: x => x.ListGamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GamePlatform_Platforms_ListPlatformsId",
                        column: x => x.ListPlatformsId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameLibraries_UserId",
                table: "GameLibraries",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameGenre_ListGenresId",
                table: "GameGenre",
                column: "ListGenresId");

            migrationBuilder.CreateIndex(
                name: "IX_GameGameLibrary_ListGameLibrariesId",
                table: "GameGameLibrary",
                column: "ListGameLibrariesId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlatform_ListPlatformsId",
                table: "GamePlatform",
                column: "ListPlatformsId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameGenre_Games_ListGamesId",
                table: "GameGenre",
                column: "ListGamesId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameGenre_Genres_ListGenresId",
                table: "GameGenre",
                column: "ListGenresId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameLibraries_AspNetUsers_UserId",
                table: "GameLibraries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
