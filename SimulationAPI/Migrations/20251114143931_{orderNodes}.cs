using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class orderNodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OSMNodeId",
                table: "PathNode",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "PathNode",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "OSMNodeId",
                table: "OSMPathNodePresets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OSMNodeId",
                table: "PathNode");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "PathNode");

            migrationBuilder.DropColumn(
                name: "OSMNodeId",
                table: "OSMPathNodePresets");
        }
    }
}
