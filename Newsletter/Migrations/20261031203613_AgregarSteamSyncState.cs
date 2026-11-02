using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Newsletter.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSteamSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamSyncStates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamSyncStates");
        }
    }
}
