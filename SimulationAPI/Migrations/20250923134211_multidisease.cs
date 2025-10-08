using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class multidisease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Simulations_Diseases_DiseaseId",
                table: "Simulations");

            migrationBuilder.DropIndex(
                name: "IX_Simulations_DiseaseId",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "Simulations");

            migrationBuilder.AddColumn<int>(
                name: "SimulationId",
                table: "Diseases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_SimulationId",
                table: "Diseases",
                column: "SimulationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Diseases_Simulations_SimulationId",
                table: "Diseases",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diseases_Simulations_SimulationId",
                table: "Diseases");

            migrationBuilder.DropIndex(
                name: "IX_Diseases_SimulationId",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "SimulationId",
                table: "Diseases");

            migrationBuilder.AddColumn<int>(
                name: "DiseaseId",
                table: "Simulations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_DiseaseId",
                table: "Simulations",
                column: "DiseaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Simulations_Diseases_DiseaseId",
                table: "Simulations",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
