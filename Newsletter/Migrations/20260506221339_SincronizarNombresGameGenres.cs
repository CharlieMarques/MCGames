using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Newsletter.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarNombresGameGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Games_gamesId",
                table: "GameGenre");

            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Genres_GenresId",
                table: "GameGenre");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre");

            migrationBuilder.DropIndex(
                name: "IX_GameGenre_gamesId",
                table: "GameGenre");

            migrationBuilder.RenameColumn(
                name: "gamesId",
                table: "GameGenre",
                newName: "GameId");

            migrationBuilder.RenameColumn(
                name: "GenresId",
                table: "GameGenre",
                newName: "GenreId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre",
                columns: new[] { "GameId", "GenreId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameGenre_GenreId",
                table: "GameGenre",
                column: "GenreId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameGenre_Games_GameId",
                table: "GameGenre",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameGenre_Genres_GenreId",
                table: "GameGenre",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Games_GameId",
                table: "GameGenre");

            migrationBuilder.DropForeignKey(
                name: "FK_GameGenre_Genres_GenreId",
                table: "GameGenre");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre");

            migrationBuilder.DropIndex(
                name: "IX_GameGenre_GenreId",
                table: "GameGenre");

            migrationBuilder.RenameColumn(
                name: "GenreId",
                table: "GameGenre",
                newName: "GenresId");

            migrationBuilder.RenameColumn(
                name: "GameId",
                table: "GameGenre",
                newName: "gamesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameGenre",
                table: "GameGenre",
                columns: new[] { "GenresId", "gamesId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameGenre_gamesId",
                table: "GameGenre",
                column: "gamesId");

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
        }
    }
}
