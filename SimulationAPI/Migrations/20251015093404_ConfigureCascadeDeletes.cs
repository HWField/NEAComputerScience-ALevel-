using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diseases_Simulations_SimulationId",
                table: "Diseases");

            migrationBuilder.DropForeignKey(
                name: "FK_Mitigations_Simulations_SimulationId",
                table: "Mitigations");

            migrationBuilder.AddColumn<int>(
                name: "MapAreaId1",
                table: "Paths",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MapAreaId1",
                table: "Buildings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paths_MapAreaId1",
                table: "Paths",
                column: "MapAreaId1");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_MapAreaId1",
                table: "Buildings",
                column: "MapAreaId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Buildings_MapAreas_MapAreaId1",
                table: "Buildings",
                column: "MapAreaId1",
                principalTable: "MapAreas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Diseases_Simulations_SimulationId",
                table: "Diseases",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mitigations_Simulations_SimulationId",
                table: "Mitigations",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Paths_MapAreas_MapAreaId1",
                table: "Paths",
                column: "MapAreaId1",
                principalTable: "MapAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buildings_MapAreas_MapAreaId1",
                table: "Buildings");

            migrationBuilder.DropForeignKey(
                name: "FK_Diseases_Simulations_SimulationId",
                table: "Diseases");

            migrationBuilder.DropForeignKey(
                name: "FK_Mitigations_Simulations_SimulationId",
                table: "Mitigations");

            migrationBuilder.DropForeignKey(
                name: "FK_Paths_MapAreas_MapAreaId1",
                table: "Paths");

            migrationBuilder.DropIndex(
                name: "IX_Paths_MapAreaId1",
                table: "Paths");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_MapAreaId1",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "MapAreaId1",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "MapAreaId1",
                table: "Buildings");

            migrationBuilder.AddForeignKey(
                name: "FK_Diseases_Simulations_SimulationId",
                table: "Diseases",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mitigations_Simulations_SimulationId",
                table: "Mitigations",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id");
        }
    }
}
