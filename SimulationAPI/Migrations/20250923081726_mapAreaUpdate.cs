using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class mapAreaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HomeId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HospitalId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_ResidentialBuildingId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_SchoolId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_StoreId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_HospitalId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Preset",
                table: "MapAreas");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "StoreId",
                table: "Agents",
                newName: "StudentAgent_WorkplaceId");

            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "Agents",
                newName: "RetailWorkerAgent_WorkplaceId");

            migrationBuilder.RenameColumn(
                name: "ResidentialBuildingId",
                table: "Agents",
                newName: "OfficeWorkerAgent_WorkplaceId");

            migrationBuilder.RenameColumn(
                name: "HomeId",
                table: "Agents",
                newName: "HouseId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_StoreId",
                table: "Agents",
                newName: "IX_Agents_StudentAgent_WorkplaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_SchoolId",
                table: "Agents",
                newName: "IX_Agents_RetailWorkerAgent_WorkplaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_ResidentialBuildingId",
                table: "Agents",
                newName: "IX_Agents_OfficeWorkerAgent_WorkplaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_HomeId",
                table: "Agents",
                newName: "IX_Agents_HouseId");

            migrationBuilder.AddColumn<int>(
                name: "PresetRefID",
                table: "MapAreas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "Buildings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingCapacity",
                table: "Buildings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents",
                column: "HouseId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_OfficeWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_RetailWorkerAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_StudentAgent_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "PresetRefID",
                table: "MapAreas");

            migrationBuilder.DropColumn(
                name: "RemainingCapacity",
                table: "Buildings");

            migrationBuilder.RenameColumn(
                name: "StudentAgent_WorkplaceId",
                table: "Agents",
                newName: "StoreId");

            migrationBuilder.RenameColumn(
                name: "RetailWorkerAgent_WorkplaceId",
                table: "Agents",
                newName: "SchoolId");

            migrationBuilder.RenameColumn(
                name: "OfficeWorkerAgent_WorkplaceId",
                table: "Agents",
                newName: "ResidentialBuildingId");

            migrationBuilder.RenameColumn(
                name: "HouseId",
                table: "Agents",
                newName: "HomeId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_StudentAgent_WorkplaceId",
                table: "Agents",
                newName: "IX_Agents_StoreId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_RetailWorkerAgent_WorkplaceId",
                table: "Agents",
                newName: "IX_Agents_SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_OfficeWorkerAgent_WorkplaceId",
                table: "Agents",
                newName: "IX_Agents_ResidentialBuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_HouseId",
                table: "Agents",
                newName: "IX_Agents_HomeId");

            migrationBuilder.AddColumn<bool>(
                name: "Preset",
                table: "MapAreas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "Buildings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_HospitalId",
                table: "Agents",
                column: "HospitalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HomeId",
                table: "Agents",
                column: "HomeId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HospitalId",
                table: "Agents",
                column: "HospitalId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_ResidentialBuildingId",
                table: "Agents",
                column: "ResidentialBuildingId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_SchoolId",
                table: "Agents",
                column: "SchoolId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_StoreId",
                table: "Agents",
                column: "StoreId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
