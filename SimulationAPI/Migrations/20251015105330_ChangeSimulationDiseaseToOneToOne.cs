using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSimulationDiseaseToOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Diseases_SimulationId",
                table: "Diseases");

            migrationBuilder.AlterColumn<int>(
                name: "SimulationId",
                table: "Diseases",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_SimulationId",
                table: "Diseases",
                column: "SimulationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Diseases_SimulationId",
                table: "Diseases");

            migrationBuilder.AlterColumn<int>(
                name: "SimulationId",
                table: "Diseases",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_SimulationId",
                table: "Diseases",
                column: "SimulationId");
        }
    }
}
