using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Buildings_MapAreas_MapAreaId1",
                table: "Buildings");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_MapAreaId1",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "MapAreaId1",
                table: "Buildings");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents",
                column: "HouseId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents",
                column: "WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents");

            migrationBuilder.AddColumn<int>(
                name: "MapAreaId1",
                table: "Buildings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_MapAreaId1",
                table: "Buildings",
                column: "MapAreaId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents",
                column: "HouseId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents",
                column: "WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Buildings_MapAreas_MapAreaId1",
                table: "Buildings",
                column: "MapAreaId1",
                principalTable: "MapAreas",
                principalColumn: "Id");
        }
    }
}
