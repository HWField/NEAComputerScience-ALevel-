using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class agentHomeWorkplaceOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents");

            migrationBuilder.AlterColumn<int>(
                name: "WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HouseId",
                table: "Agents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents",
                column: "HouseId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents",
                column: "WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents");

            migrationBuilder.AlterColumn<int>(
                name: "WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HouseId",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HouseId",
                table: "Agents",
                column: "HouseId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents",
                column: "WorkplaceId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
