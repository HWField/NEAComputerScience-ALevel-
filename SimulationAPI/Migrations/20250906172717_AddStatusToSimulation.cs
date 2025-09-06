using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Locations",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Simulations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Locations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Lon",
                table: "Locations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "OSMId",
                table: "Locations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SimulationId",
                table: "Locations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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
                name: "HealthStatus",
                table: "Agents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HomeLocationId",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Agents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WorkLocationId",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_SimulationId",
                table: "Locations",
                column: "SimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_HomeLocationId",
                table: "Agents",
                column: "HomeLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_WorkLocationId",
                table: "Agents",
                column: "WorkLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Locations_HomeLocationId",
                table: "Agents",
                column: "HomeLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Locations_WorkLocationId",
                table: "Agents",
                column: "WorkLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Simulations_SimulationId",
                table: "Locations",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Locations_HomeLocationId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Locations_WorkLocationId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Simulations_SimulationId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_SimulationId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Agents_HomeLocationId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_WorkLocationId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "OSMId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "SimulationId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CurrentLat",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CurrentLon",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "HomeLocationId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "WorkLocationId",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Locations",
                newName: "Name");
        }
    }
}
