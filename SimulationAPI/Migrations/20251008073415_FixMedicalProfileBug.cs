using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixMedicalProfileBug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_MedicalProfiles_MedicalProfileId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_MedicalProfileId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "MedicalProfileId",
                table: "Agents");

            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "MedicalProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalProfiles_AgentId",
                table: "MedicalProfiles",
                column: "AgentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalProfiles_Agents_AgentId",
                table: "MedicalProfiles",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalProfiles_Agents_AgentId",
                table: "MedicalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_MedicalProfiles_AgentId",
                table: "MedicalProfiles");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "MedicalProfiles");

            migrationBuilder.AddColumn<int>(
                name: "MedicalProfileId",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_MedicalProfileId",
                table: "Agents",
                column: "MedicalProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_MedicalProfiles_MedicalProfileId",
                table: "Agents",
                column: "MedicalProfileId",
                principalTable: "MedicalProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
