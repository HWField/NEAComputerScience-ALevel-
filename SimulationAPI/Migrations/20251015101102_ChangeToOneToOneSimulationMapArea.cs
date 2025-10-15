using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToOneToOneSimulationMapArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations",
                column: "MapAreaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations",
                column: "MapAreaId");
        }
    }
}
