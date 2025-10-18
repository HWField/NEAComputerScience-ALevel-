using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class agentClassErrorForgotToUnmapCurrentBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_CurrentBuildingId1",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_CurrentBuildingId1",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CurrentBuildingId1",
                table: "Agents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentBuildingId1",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_CurrentBuildingId1",
                table: "Agents",
                column: "CurrentBuildingId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_CurrentBuildingId1",
                table: "Agents",
                column: "CurrentBuildingId1",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
