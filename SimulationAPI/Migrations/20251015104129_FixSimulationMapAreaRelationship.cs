using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixSimulationMapAreaRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Simulations_MapAreas_MapAreaId",
                table: "Simulations");

            migrationBuilder.DropIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "MapAreaId",
                table: "Simulations");

            migrationBuilder.AddColumn<int>(
                name: "SimulationId",
                table: "MapAreas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MapAreas_SimulationId",
                table: "MapAreas",
                column: "SimulationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MapAreas_Simulations_SimulationId",
                table: "MapAreas",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapAreas_Simulations_SimulationId",
                table: "MapAreas");

            migrationBuilder.DropIndex(
                name: "IX_MapAreas_SimulationId",
                table: "MapAreas");

            migrationBuilder.DropColumn(
                name: "SimulationId",
                table: "MapAreas");

            migrationBuilder.AddColumn<int>(
                name: "MapAreaId",
                table: "Simulations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations",
                column: "MapAreaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Simulations_MapAreas_MapAreaId",
                table: "Simulations",
                column: "MapAreaId",
                principalTable: "MapAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
