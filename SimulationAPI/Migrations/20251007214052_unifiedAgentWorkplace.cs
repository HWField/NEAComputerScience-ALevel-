using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class unifiedAgentWorkplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_OfficeWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_RetailWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_StudentAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_OfficeWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_RetailWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_StudentAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "OfficeWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "RetailWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "StudentAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.AlterColumn<int>(
                name: "WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "OfficeWorkerAgent_WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetailWorkerAgent_WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentAgent_WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OfficeWorkerAgent_WorkplaceId",
                table: "Agents",
                column: "OfficeWorkerAgent_WorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_RetailWorkerAgent_WorkplaceId",
                table: "Agents",
                column: "RetailWorkerAgent_WorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_StudentAgent_WorkplaceId",
                table: "Agents",
                column: "StudentAgent_WorkplaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_OfficeWorkerAgent_WorkplaceId",
                table: "Agents",
                column: "OfficeWorkerAgent_WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_RetailWorkerAgent_WorkplaceId",
                table: "Agents",
                column: "RetailWorkerAgent_WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_StudentAgent_WorkplaceId",
                table: "Agents",
                column: "StudentAgent_WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
