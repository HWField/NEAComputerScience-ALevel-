using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddInfectionMetricsToSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalDeaths",
                table: "SimulationSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInfections",
                table: "SimulationSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRecoveries",
                table: "SimulationSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalDeaths",
                table: "SimulationSnapshots");

            migrationBuilder.DropColumn(
                name: "TotalInfections",
                table: "SimulationSnapshots");

            migrationBuilder.DropColumn(
                name: "TotalRecoveries",
                table: "SimulationSnapshots");
        }
    }
}
