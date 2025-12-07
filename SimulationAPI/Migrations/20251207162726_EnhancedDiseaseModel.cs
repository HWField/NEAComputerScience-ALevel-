using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedDiseaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HospitalSeekingRate",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImmunityDuration",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaskEffectiveness",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ReinfectionResistance",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SevereSymptomsRate",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SymptomaticRate",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "VaccineEffectiveness",
                table: "Diseases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "HospitalSeekingRate",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImmunityDuration",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaskEffectiveness",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ReinfectionResistance",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SevereSymptomsRate",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SymptomaticRate",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "VaccineEffectiveness",
                table: "DiseasePresets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HospitalSeekingRate",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "ImmunityDuration",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "MaskEffectiveness",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "ReinfectionResistance",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "SevereSymptomsRate",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "SymptomaticRate",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "VaccineEffectiveness",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "HospitalSeekingRate",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "ImmunityDuration",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "MaskEffectiveness",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "ReinfectionResistance",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "SevereSymptomsRate",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "SymptomaticRate",
                table: "DiseasePresets");

            migrationBuilder.DropColumn(
                name: "VaccineEffectiveness",
                table: "DiseasePresets");
        }
    }
}
