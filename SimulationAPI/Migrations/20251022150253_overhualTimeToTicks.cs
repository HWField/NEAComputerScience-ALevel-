using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class overhualTimeToTicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoutineEvent_Agents_AgentId",
                table: "RoutineEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutineEvent_Buildings_BuildingId",
                table: "RoutineEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoutineEvent",
                table: "RoutineEvent");

            migrationBuilder.RenameTable(
                name: "RoutineEvent",
                newName: "RoutineEvents");

            migrationBuilder.RenameColumn(
                name: "TickLengthMinutes",
                table: "Simulations",
                newName: "TickLength");

            migrationBuilder.RenameIndex(
                name: "IX_RoutineEvent_BuildingId",
                table: "RoutineEvents",
                newName: "IX_RoutineEvents_BuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_RoutineEvent_AgentId",
                table: "RoutineEvents",
                newName: "IX_RoutineEvents_AgentId");

            migrationBuilder.AddColumn<string>(
                name: "CachedPath",
                table: "RoutineEvents",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CachedTravelDuration",
                table: "RoutineEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoutineEvents",
                table: "RoutineEvents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineEvents_Agents_AgentId",
                table: "RoutineEvents",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineEvents_Buildings_BuildingId",
                table: "RoutineEvents",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoutineEvents_Agents_AgentId",
                table: "RoutineEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutineEvents_Buildings_BuildingId",
                table: "RoutineEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoutineEvents",
                table: "RoutineEvents");

            migrationBuilder.DropColumn(
                name: "CachedPath",
                table: "RoutineEvents");

            migrationBuilder.DropColumn(
                name: "CachedTravelDuration",
                table: "RoutineEvents");

            migrationBuilder.RenameTable(
                name: "RoutineEvents",
                newName: "RoutineEvent");

            migrationBuilder.RenameColumn(
                name: "TickLength",
                table: "Simulations",
                newName: "TickLengthMinutes");

            migrationBuilder.RenameIndex(
                name: "IX_RoutineEvents_BuildingId",
                table: "RoutineEvent",
                newName: "IX_RoutineEvent_BuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_RoutineEvents_AgentId",
                table: "RoutineEvent",
                newName: "IX_RoutineEvent_AgentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoutineEvent",
                table: "RoutineEvent",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineEvent_Agents_AgentId",
                table: "RoutineEvent",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineEvent_Buildings_BuildingId",
                table: "RoutineEvent",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
