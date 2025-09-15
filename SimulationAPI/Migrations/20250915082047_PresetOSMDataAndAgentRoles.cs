using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimulationAPI.Migrations
{
    /// <inheritdoc />
    public partial class PresetOSMDataAndAgentRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Locations_HomeLocationId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Locations_WorkLocationId",
                table: "Agents");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "WorkLocationId",
                table: "Agents",
                newName: "MedicalProfileId");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Agents",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "HomeLocationId",
                table: "Agents",
                newName: "HomeId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_WorkLocationId",
                table: "Agents",
                newName: "IX_Agents_MedicalProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_HomeLocationId",
                table: "Agents",
                newName: "IX_Agents_HomeId");

            migrationBuilder.AddColumn<long>(
                name: "CurrentTick",
                table: "Simulations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "DiseaseId",
                table: "Simulations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MapAreaId",
                table: "Simulations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TickLengthMinutes",
                table: "Simulations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AgentType",
                table: "Agents",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HospitalId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResidentialBuildingId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceLocationId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkplaceId",
                table: "Agents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Diseases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InfectionRate = table.Column<double>(type: "double precision", nullable: false),
                    RecoveryRate = table.Column<double>(type: "double precision", nullable: false),
                    MortalityRate = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diseases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapAreaPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Radius = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapAreaPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Preset = table.Column<bool>(type: "boolean", nullable: false),
                    CenterLat = table.Column<double>(type: "double precision", nullable: false),
                    CenterLon = table.Column<double>(type: "double precision", nullable: false),
                    RadiusKm = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Immunity = table.Column<double>(type: "double precision", nullable: false),
                    HasChronicCondition = table.Column<bool>(type: "boolean", nullable: false),
                    InfectionSusceptibility = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mitigations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MitigationType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    SimulationId = table.Column<int>(type: "integer", nullable: true),
                    Efficacy = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mitigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mitigations_Simulations_SimulationId",
                        column: x => x.SimulationId,
                        principalTable: "Simulations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PathNode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Lon = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathNode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tick = table.Column<int>(type: "integer", nullable: false),
                    HealthyCount = table.Column<int>(type: "integer", nullable: false),
                    InfectedCount = table.Column<int>(type: "integer", nullable: false),
                    RecoveredCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OSMBuildingPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    OSMId = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    MapAreaPresetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OSMBuildingPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OSMBuildingPresets_MapAreaPresets_MapAreaPresetId",
                        column: x => x.MapAreaPresetId,
                        principalTable: "MapAreaPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OSMPathPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OSMId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    SpeedModifier = table.Column<double>(type: "double precision", nullable: false),
                    IsOneWay = table.Column<bool>(type: "boolean", nullable: false),
                    MapAreaPresetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OSMPathPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OSMPathPresets_MapAreaPresets_MapAreaPresetId",
                        column: x => x.MapAreaPresetId,
                        principalTable: "MapAreaPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    MapAreaId = table.Column<int>(type: "integer", nullable: false),
                    BuildingType = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: true),
                    NumBeds = table.Column<int>(type: "integer", nullable: true),
                    HasICU = table.Column<bool>(type: "boolean", nullable: true),
                    HasCafeteria = table.Column<bool>(type: "boolean", nullable: true),
                    HasFood = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buildings_MapAreas_MapAreaId",
                        column: x => x.MapAreaId,
                        principalTable: "MapAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Paths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SpeedModifier = table.Column<double>(type: "double precision", nullable: false),
                    IsOneWay = table.Column<bool>(type: "boolean", nullable: false),
                    MapAreaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paths_MapAreas_MapAreaId",
                        column: x => x.MapAreaId,
                        principalTable: "MapAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OSMPathNodePresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    OSMPathPresetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OSMPathNodePresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OSMPathNodePresets_OSMPathPresets_OSMPathPresetId",
                        column: x => x.OSMPathPresetId,
                        principalTable: "OSMPathPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoutineEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentId = table.Column<int>(type: "integer", nullable: false),
                    DayIndex = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BuildingId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineEvent_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineEvent_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Simulations_DiseaseId",
                table: "Simulations",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations",
                column: "MapAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_HospitalId",
                table: "Agents",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ResidentialBuildingId",
                table: "Agents",
                column: "ResidentialBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_SchoolId",
                table: "Agents",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ServiceLocationId",
                table: "Agents",
                column: "ServiceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_StoreId",
                table: "Agents",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_WorkplaceId",
                table: "Agents",
                column: "WorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_MapAreaId",
                table: "Buildings",
                column: "MapAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mitigations_SimulationId",
                table: "Mitigations",
                column: "SimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_OSMBuildingPresets_MapAreaPresetId",
                table: "OSMBuildingPresets",
                column: "MapAreaPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_OSMPathNodePresets_OSMPathPresetId",
                table: "OSMPathNodePresets",
                column: "OSMPathPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_OSMPathPresets_MapAreaPresetId",
                table: "OSMPathPresets",
                column: "MapAreaPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_PathPathNode_NodesId",
                table: "PathPathNode",
                column: "NodesId");

            migrationBuilder.CreateIndex(
                name: "IX_Paths_MapAreaId",
                table: "Paths",
                column: "MapAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineEvent_AgentId",
                table: "RoutineEvent",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineEvent_BuildingId",
                table: "RoutineEvent",
                column: "BuildingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HomeId",
                table: "Agents",
                column: "HomeId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_HospitalId",
                table: "Agents",
                column: "HospitalId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_ResidentialBuildingId",
                table: "Agents",
                column: "ResidentialBuildingId",
                principalTable: "Buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_SchoolId",
                table: "Agents",
                column: "SchoolId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_ServiceLocationId",
                table: "Agents",
                column: "ServiceLocationId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Buildings_StoreId",
                table: "Agents",
                column: "StoreId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_MedicalProfiles_MedicalProfileId",
                table: "Agents",
                column: "MedicalProfileId",
                principalTable: "MedicalProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Simulations_Diseases_DiseaseId",
                table: "Simulations",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Simulations_MapAreas_MapAreaId",
                table: "Simulations",
                column: "MapAreaId",
                principalTable: "MapAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HomeId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_HospitalId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_ResidentialBuildingId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_SchoolId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_ServiceLocationId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_StoreId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Buildings_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_MedicalProfiles_MedicalProfileId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Simulations_Diseases_DiseaseId",
                table: "Simulations");

            migrationBuilder.DropForeignKey(
                name: "FK_Simulations_MapAreas_MapAreaId",
                table: "Simulations");

            migrationBuilder.DropTable(
                name: "Diseases");

            migrationBuilder.DropTable(
                name: "MedicalProfiles");

            migrationBuilder.DropTable(
                name: "Mitigations");

            migrationBuilder.DropTable(
                name: "OSMBuildingPresets");

            migrationBuilder.DropTable(
                name: "OSMPathNodePresets");

            migrationBuilder.DropTable(
                name: "PathPathNode");

            migrationBuilder.DropTable(
                name: "RoutineEvent");

            migrationBuilder.DropTable(
                name: "SimulationSnapshots");

            migrationBuilder.DropTable(
                name: "OSMPathPresets");

            migrationBuilder.DropTable(
                name: "PathNode");

            migrationBuilder.DropTable(
                name: "Paths");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "MapAreaPresets");

            migrationBuilder.DropTable(
                name: "MapAreas");

            migrationBuilder.DropIndex(
                name: "IX_Simulations_DiseaseId",
                table: "Simulations");

            migrationBuilder.DropIndex(
                name: "IX_Simulations_MapAreaId",
                table: "Simulations");

            migrationBuilder.DropIndex(
                name: "IX_Agents_HospitalId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_ResidentialBuildingId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_SchoolId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_ServiceLocationId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_StoreId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_WorkplaceId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CurrentTick",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "MapAreaId",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "TickLengthMinutes",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "AgentType",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "ResidentialBuildingId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "ServiceLocationId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "WorkplaceId",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Agents",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "MedicalProfileId",
                table: "Agents",
                newName: "WorkLocationId");

            migrationBuilder.RenameColumn(
                name: "HomeId",
                table: "Agents",
                newName: "HomeLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_MedicalProfileId",
                table: "Agents",
                newName: "IX_Agents_WorkLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Agents_HomeId",
                table: "Agents",
                newName: "IX_Agents_HomeLocationId");

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "Agents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationId = table.Column<int>(type: "integer", nullable: false),
                    Lat = table.Column<double>(type: "double precision", nullable: false),
                    Lon = table.Column<double>(type: "double precision", nullable: false),
                    OSMId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Simulations_SimulationId",
                        column: x => x.SimulationId,
                        principalTable: "Simulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_SimulationId",
                table: "Locations",
                column: "SimulationId");

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
        }
    }
}
