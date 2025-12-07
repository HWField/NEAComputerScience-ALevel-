using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAgentSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Immunity",
                table: "AgentSnapshots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<byte>(
                name: "InfectionCount",
                table: "AgentSnapshots",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "IsVaccinated",
                table: "AgentSnapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "SymptomSeverity",
                table: "AgentSnapshots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<bool>(
                name: "WearsMask",
                table: "AgentSnapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Immunity",
                table: "AgentSnapshots");

            migrationBuilder.DropColumn(
                name: "InfectionCount",
                table: "AgentSnapshots");

            migrationBuilder.DropColumn(
                name: "IsVaccinated",
                table: "AgentSnapshots");

            migrationBuilder.DropColumn(
                name: "SymptomSeverity",
                table: "AgentSnapshots");

            migrationBuilder.DropColumn(
                name: "WearsMask",
                table: "AgentSnapshots");
        }
    }
}
