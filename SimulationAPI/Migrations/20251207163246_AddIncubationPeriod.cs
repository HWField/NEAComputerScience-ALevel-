using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIncubationPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IncubationPeriod",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "InfectiousPeriod",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "IncubationPeriod",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "InfectiousPeriod",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncubationPeriod",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "InfectiousPeriod",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "IncubationPeriod",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "InfectiousPeriod",
                table: "DiseasePresets");
        }
    }
}
