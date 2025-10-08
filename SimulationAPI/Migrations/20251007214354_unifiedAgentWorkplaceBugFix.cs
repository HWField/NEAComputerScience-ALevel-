using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class unifiedAgentWorkplaceBugFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_ServiceLocationId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_ServiceLocationId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "ServiceLocationId",
                table: "Agents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceLocationId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ServiceLocationId",
                table: "Agents",
                column: "ServiceLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_ServiceLocationId",
                table: "Agents",
                column: "ServiceLocationId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
