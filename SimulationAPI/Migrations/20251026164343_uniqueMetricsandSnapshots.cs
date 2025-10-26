using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class uniqueMetricsandSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimulationSnapshots_SimulationId",
                table: "SimulationSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SimulationMetrics_SimulationId",
                table: "SimulationMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSnapshots_SimulationId_Tick",
                table: "SimulationSnapshots",
                columns: new[] { "SimulationId", "Tick" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetrics_SimulationId_Tick",
                table: "SimulationMetrics",
                columns: new[] { "SimulationId", "Tick" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimulationSnapshots_SimulationId_Tick",
                table: "SimulationSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SimulationMetrics_SimulationId_Tick",
                table: "SimulationMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSnapshots_SimulationId",
                table: "SimulationSnapshots",
                column: "SimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetrics_SimulationId",
                table: "SimulationMetrics",
                column: "SimulationId");
        }
    }
}
