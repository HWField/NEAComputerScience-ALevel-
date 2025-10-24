using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class pluralisationFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentSnapshot_Agents_AgentId",
                table: "AgentSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentSnapshot_Buildings_CurrentBuildingId",
                table: "AgentSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentSnapshot_SimulationSnapshots_SimulationSnapshotId",
                table: "AgentSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingSnapshot_Buildings_BuildingId",
                table: "BuildingSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingSnapshot_SimulationSnapshots_SimulationSnapshotId",
                table: "BuildingSnapshot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BuildingSnapshot",
                table: "BuildingSnapshot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentSnapshot",
                table: "AgentSnapshot");

            migrationBuilder.RenameTable(
                name: "BuildingSnapshot",
                newName: "BuildingSnapshots");

            migrationBuilder.RenameTable(
                name: "AgentSnapshot",
                newName: "AgentSnapshots");

            migrationBuilder.RenameIndex(
                name: "IX_BuildingSnapshot_SimulationSnapshotId",
                table: "BuildingSnapshots",
                newName: "IX_BuildingSnapshots_SimulationSnapshotId");

            migrationBuilder.RenameIndex(
                name: "IX_BuildingSnapshot_BuildingId",
                table: "BuildingSnapshots",
                newName: "IX_BuildingSnapshots_BuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentSnapshot_SimulationSnapshotId",
                table: "AgentSnapshots",
                newName: "IX_AgentSnapshots_SimulationSnapshotId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentSnapshot_CurrentBuildingId",
                table: "AgentSnapshots",
                newName: "IX_AgentSnapshots_CurrentBuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentSnapshot_AgentId",
                table: "AgentSnapshots",
                newName: "IX_AgentSnapshots_AgentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BuildingSnapshots",
                table: "BuildingSnapshots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentSnapshots",
                table: "AgentSnapshots",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSnapshots_Agents_AgentId",
                table: "AgentSnapshots",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSnapshots_Buildings_CurrentBuildingId",
                table: "AgentSnapshots",
                column: "CurrentBuildingId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSnapshots_SimulationSnapshots_SimulationSnapshotId",
                table: "AgentSnapshots",
                column: "SimulationSnapshotId",
                principalTable: "SimulationSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingSnapshots_Buildings_BuildingId",
                table: "BuildingSnapshots",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingSnapshots_SimulationSnapshots_SimulationSnapshotId",
                table: "BuildingSnapshots",
                column: "SimulationSnapshotId",
                principalTable: "SimulationSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentSnapshots_Agents_AgentId",
                table: "AgentSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentSnapshots_Buildings_CurrentBuildingId",
                table: "AgentSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentSnapshots_SimulationSnapshots_SimulationSnapshotId",
                table: "AgentSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingSnapshots_Buildings_BuildingId",
                table: "BuildingSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingSnapshots_SimulationSnapshots_SimulationSnapshotId",
                table: "BuildingSnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BuildingSnapshots",
                table: "BuildingSnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentSnapshots",
                table: "AgentSnapshots");

            migrationBuilder.RenameTable(
                name: "BuildingSnapshots",
                newName: "BuildingSnapshot");

            migrationBuilder.RenameTable(
                name: "AgentSnapshots",
                newName: "AgentSnapshot");

            migrationBuilder.RenameIndex(
                name: "IX_BuildingSnapshots_SimulationSnapshotId",
                table: "BuildingSnapshot",
                newName: "IX_BuildingSnapshot_SimulationSnapshotId");

            migrationBuilder.RenameIndex(
                name: "IX_BuildingSnapshots_BuildingId",
                table: "BuildingSnapshot",
                newName: "IX_BuildingSnapshot_BuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentSnapshots_SimulationSnapshotId",
                table: "AgentSnapshot",
                newName: "IX_AgentSnapshot_SimulationSnapshotId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentSnapshots_CurrentBuildingId",
                table: "AgentSnapshot",
                newName: "IX_AgentSnapshot_CurrentBuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentSnapshots_AgentId",
                table: "AgentSnapshot",
                newName: "IX_AgentSnapshot_AgentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BuildingSnapshot",
                table: "BuildingSnapshot",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentSnapshot",
                table: "AgentSnapshot",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSnapshot_Agents_AgentId",
                table: "AgentSnapshot",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSnapshot_Buildings_CurrentBuildingId",
                table: "AgentSnapshot",
                column: "CurrentBuildingId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentSnapshot_SimulationSnapshots_SimulationSnapshotId",
                table: "AgentSnapshot",
                column: "SimulationSnapshotId",
                principalTable: "SimulationSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingSnapshot_Buildings_BuildingId",
                table: "BuildingSnapshot",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingSnapshot_SimulationSnapshots_SimulationSnapshotId",
                table: "BuildingSnapshot",
                column: "SimulationSnapshotId",
                principalTable: "SimulationSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
