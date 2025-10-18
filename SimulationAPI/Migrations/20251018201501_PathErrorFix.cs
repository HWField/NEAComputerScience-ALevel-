using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class PathErrorFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PathPathNode");

            migrationBuilder.AddColumn<int>(
                name: "PathNodeId",
                table: "Paths",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PathId",
                table: "PathNode",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Paths_PathNodeId",
                table: "Paths",
                column: "PathNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PathNode_PathId",
                table: "PathNode",
                column: "PathId");

            migrationBuilder.AddForeignKey(
                name: "FK_PathNode_Paths_PathId",
                table: "PathNode",
                column: "PathId",
                principalTable: "Paths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Paths_PathNode_PathNodeId",
                table: "Paths",
                column: "PathNodeId",
                principalTable: "PathNode",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PathNode_Paths_PathId",
                table: "PathNode");

            migrationBuilder.DropForeignKey(
                name: "FK_Paths_PathNode_PathNodeId",
                table: "Paths");

            migrationBuilder.DropIndex(
                name: "IX_Paths_PathNodeId",
                table: "Paths");

            migrationBuilder.DropIndex(
                name: "IX_PathNode_PathId",
                table: "PathNode");

            migrationBuilder.DropColumn(
                name: "PathNodeId",
                table: "Paths");

            migrationBuilder.DropColumn(
                name: "PathId",
                table: "PathNode");

            migrationBuilder.CreateTable(
                name: "PathPathNode",
                columns: table => new
                {
                    ConnectedPathsId = table.Column<int>(type: "integer", nullable: false),
                    NodesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathPathNode", x => new { x.ConnectedPathsId, x.NodesId });
                    table.ForeignKey(
                        name: "FK_PathPathNode_PathNode_NodesId",
                        column: x => x.NodesId,
                        principalTable: "PathNode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathPathNode_Paths_ConnectedPathsId",
                        column: x => x.ConnectedPathsId,
                        principalTable: "Paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PathPathNode_NodesId",
                table: "PathPathNode",
                column: "NodesId");
        }
    }
}
