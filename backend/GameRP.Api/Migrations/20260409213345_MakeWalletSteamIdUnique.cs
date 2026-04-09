using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameRP.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeWalletSteamIdUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_SteamId",
                table: "Wallets");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_SteamId",
                table: "Wallets",
                column: "SteamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_SteamId",
                table: "Wallets");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_SteamId",
                table: "Wallets",
                column: "SteamId");
        }
    }
}
