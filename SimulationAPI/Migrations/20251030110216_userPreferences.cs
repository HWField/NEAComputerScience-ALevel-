using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class userPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StreamerMode",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StreamerMode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Users");
        }
    }
}
