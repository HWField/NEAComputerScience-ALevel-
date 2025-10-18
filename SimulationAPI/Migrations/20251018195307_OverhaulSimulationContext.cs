using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class OverhaulSimulationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthyCount",
                table: "SimulationSnapshots");

            migrationBuilder.DropColumn(
                name: "CurrentTick",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "CurrentLat",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CurrentLon",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "RecoveredCount",
                table: "SimulationSnapshots",
                newName: "SimulationId1");

            migrationBuilder.RenameColumn(
                name: "InfectedCount",
                table: "SimulationSnapshots",
                newName: "SimulationId");

            migrationBuilder.AlterColumn<long>(
                name: "Tick",
                table: "SimulationSnapshots",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SimulationSnapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentBuildingId1",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AgentSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentId = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    HealthState = table.Column<string>(type: "text", nullable: false),
                    InfectionProgress = table.Column<double>(type: "double precision", nullable: false),
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentBuildingId = table.Column<int>(type: "integer", nullable: true),
                    SimulationSnapshotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSnapshot_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentSnapshot_Buildings_CurrentBuildingId",
                        column: x => x.CurrentBuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentSnapshot_SimulationSnapshots_SimulationSnapshotId",
                        column: x => x.SimulationSnapshotId,
                        principalTable: "SimulationSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuildingSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<int>(type: "integer", nullable: false),
                    CurrentOccupancy = table.Column<int>(type: "integer", nullable: false),
                    SimulationSnapshotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingSnapshot_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BuildingSnapshot_SimulationSnapshots_SimulationSnapshotId",
                        column: x => x.SimulationSnapshotId,
                        principalTable: "SimulationSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSnapshots_SimulationId",
                table: "SimulationSnapshots",
                column: "SimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSnapshots_SimulationId1",
                table: "SimulationSnapshots",
                column: "SimulationId1");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_CurrentBuildingId1",
                table: "Agents",
                column: "CurrentBuildingId1");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSnapshot_AgentId",
                table: "AgentSnapshot",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSnapshot_CurrentBuildingId",
                table: "AgentSnapshot",
                column: "CurrentBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSnapshot_SimulationSnapshotId",
                table: "AgentSnapshot",
                column: "SimulationSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingSnapshot_BuildingId",
                table: "BuildingSnapshot",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingSnapshot_SimulationSnapshotId",
                table: "BuildingSnapshot",
                column: "SimulationSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_CurrentBuildingId1",
                table: "Agents",
                column: "CurrentBuildingId1",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationSnapshots_Simulations_SimulationId",
                table: "SimulationSnapshots",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationSnapshots_Simulations_SimulationId1",
                table: "SimulationSnapshots",
                column: "SimulationId1",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_CurrentBuildingId1",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationSnapshots_Simulations_SimulationId",
                table: "SimulationSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationSnapshots_Simulations_SimulationId1",
                table: "SimulationSnapshots");

            migrationBuilder.DropTable(
                name: "AgentSnapshot");

            migrationBuilder.DropTable(
                name: "BuildingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_SimulationSnapshots_SimulationId",
                table: "SimulationSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SimulationSnapshots_SimulationId1",
                table: "SimulationSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Agents_CurrentBuildingId1",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SimulationSnapshots");

            migrationBuilder.DropColumn(
                name: "CurrentBuildingId1",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "SimulationId1",
                table: "SimulationSnapshots",
                newName: "RecoveredCount");

            migrationBuilder.RenameColumn(
                name: "SimulationId",
                table: "SimulationSnapshots",
                newName: "InfectedCount");

            migrationBuilder.AlterColumn<int>(
                name: "Tick",
                table: "SimulationSnapshots",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "HealthyCount",
                table: "SimulationSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "CurrentTick",
                table: "Simulations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLat",
                table: "Agents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLon",
                table: "Agents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Agents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
