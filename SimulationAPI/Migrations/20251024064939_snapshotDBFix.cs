using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class snapshotDBFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationSnapshots_Simulations_SimulationId1",
                table: "SimulationSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SimulationSnapshots_SimulationId1",
                table: "SimulationSnapshots");

            migrationBuilder.DropColumn(
                name: "SimulationId1",
                table: "SimulationSnapshots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SimulationId1",
                table: "SimulationSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSnapshots_SimulationId1",
                table: "SimulationSnapshots",
                column: "SimulationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationSnapshots_Simulations_SimulationId1",
                table: "SimulationSnapshots",
                column: "SimulationId1",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
