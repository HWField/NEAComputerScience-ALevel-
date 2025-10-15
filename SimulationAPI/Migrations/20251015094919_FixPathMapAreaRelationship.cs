using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixPathMapAreaRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paths_MapAreas_MapAreaId1",
                table: "Paths");

            migrationBuilder.DropIndex(
                name: "IX_Paths_MapAreaId1",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "MapAreaId1",
                table: "Paths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MapAreaId1",
                table: "Paths",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Paths_MapAreaId1",
                table: "Paths",
                column: "MapAreaId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Paths_MapAreas_MapAreaId1",
                table: "Paths",
                column: "MapAreaId1",
                principalTable: "MapAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
