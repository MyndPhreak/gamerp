using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameRP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterCreationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Age",
                table: "Players",
                type: "real",
                nullable: false,
                defaultValue: 0.5f);

            migrationBuilder.AddColumn<string>(
                name: "ClothingList",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "DateOfBirth",
                table: "Players",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Players",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedCharacterCreation",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "Players",
                type: "real",
                nullable: false,
                defaultValue: 0.5f);

            migrationBuilder.AddColumn<float>(
                name: "SkinTone",
                table: "Players",
                type: "real",
                nullable: false,
                defaultValue: 0.5f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ClothingList",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "HasCompletedCharacterCreation",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SkinTone",
                table: "Players");
        }
    }
}
