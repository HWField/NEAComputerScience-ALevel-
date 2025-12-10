using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMitigationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Efficacy",
                table: "Mitigations",
                newName: "VaccinationRate");

            migrationBuilder.AlterColumn<int>(
                name: "SimulationId",
                table: "Mitigations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AdoptionRate",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ComplianceRate",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ContactReduction",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Mitigations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DaysToFullEffectiveness",
                table: "Mitigations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EffectivenessModifier",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Mitigations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Mitigations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SocialDistancingMitigation_ComplianceRate",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SymptomThreshold",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Mitigations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "VaccinationMitigation_EffectivenessModifier",
                table: "Mitigations",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdoptionRate",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "ComplianceRate",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "ContactReduction",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "DaysToFullEffectiveness",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "EffectivenessModifier",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "SocialDistancingMitigation_ComplianceRate",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "SymptomThreshold",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "VaccinationMitigation_EffectivenessModifier",
                table: "Mitigations");

            migrationBuilder.RenameColumn(
                name: "VaccinationRate",
                table: "Mitigations",
                newName: "Efficacy");

            migrationBuilder.AlterColumn<int>(
                name: "SimulationId",
                table: "Mitigations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
