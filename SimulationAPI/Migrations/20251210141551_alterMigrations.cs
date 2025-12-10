using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class alterMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactReduction",
                table: "Mitigations");

            migrationBuilder.DropColumn(
                name: "SocialDistancingMitigation_ComplianceRate",
                table: "Mitigations");

            migrationBuilder.RenameColumn(
                name: "IsQuarantined",
                table: "AgentSnapshots",
                newName: "isIsolating");

            migrationBuilder.AlterColumn<string>(
                name: "MitigationType",
                table: "Mitigations",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isIsolating",
                table: "AgentSnapshots",
                newName: "IsQuarantined");

            migrationBuilder.AlterColumn<string>(
                name: "MitigationType",
                table: "Mitigations",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<double>(
                name: "ContactReduction",
                table: "Mitigations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SocialDistancingMitigation_ComplianceRate",
                table: "Mitigations",
                type: "double precision",
                nullable: true);
        }
    }
}
