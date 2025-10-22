using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class userGlobalPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "MapAreaPresets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiseasePresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InfectionRate = table.Column<double>(type: "double precision", nullable: false),
                    RecoveryRate = table.Column<double>(type: "double precision", nullable: false),
                    MortalityRate = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseasePresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseasePresets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapAreaPresets_UserId",
                table: "MapAreaPresets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseasePresets_UserId",
                table: "DiseasePresets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MapAreaPresets_Users_UserId",
                table: "MapAreaPresets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapAreaPresets_Users_UserId",
                table: "MapAreaPresets");

            migrationBuilder.DropTable(
                name: "DiseasePresets");

            migrationBuilder.DropIndex(
                name: "IX_MapAreaPresets_UserId",
                table: "MapAreaPresets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MapAreaPresets");
        }
    }
}
