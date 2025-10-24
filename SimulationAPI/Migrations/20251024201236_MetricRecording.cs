using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class MetricRecording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimulationMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationId = table.Column<int>(type: "integer", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false),
                    TotalAgents = table.Column<int>(type: "integer", nullable: false),
                    Infected = table.Column<int>(type: "integer", nullable: false),
                    Recovered = table.Column<int>(type: "integer", nullable: false),
                    Dead = table.Column<int>(type: "integer", nullable: false),
                    Quarantined = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationMetrics_Simulations_SimulationId",
                        column: x => x.SimulationId,
                        principalTable: "Simulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetrics_SimulationId",
                table: "SimulationMetrics",
                column: "SimulationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulationMetrics");
        }
    }
}
