using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySimulationAPI.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace MySimulationAPI.Data
{
    public class SimDbContext : DbContext
    {
        public SimDbContext(DbContextOptions<SimDbContext> options) : base(options) { }

        // User related models
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // Simulation related models
        public DbSet<Simulation> Simulations { get; set; } = null!;
        public DbSet<Agent> Agents { get; set; } = null!;
        public DbSet<RoutineEvent> RoutineEvents { get; set; } = null!;
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<Models.Path> Paths { get; set; } = null!;
        public DbSet<Disease> Diseases { get; set; } = null!;
        public DbSet<DiseasePreset> DiseasePresets { get; set; } = null!;
        public DbSet<MedicalProfile> MedicalProfiles { get; set; } = null!;
        public DbSet<Mitigation> Mitigations { get; set; } = null!;
        public DbSet<SimulationMetric> SimulationMetrics { get; set; } = null!;
        public DbSet<SimulationSnapshot> SimulationSnapshots { get; set; } = null!;
        public DbSet<AgentSnapshot> AgentSnapshots { get; set; } = null!;
        public DbSet<BuildingSnapshot> BuildingSnapshots { get; set; } = null!;
        public DbSet<MapArea> MapAreas { get; set; } = null!;

        // Preset models
        public DbSet<MapAreaPreset> MapAreaPresets { get; set; } = null!;
        public DbSet<OSMBuildingPreset> OSMBuildingPresets { get; set; } = null!;
        public DbSet<OSMPathPreset> OSMPathPresets { get; set; } = null!;
        public DbSet<OSMPathNodePreset> OSMPathNodePresets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure inheritance for Agent
            modelBuilder.Entity<Agent>()
                .HasDiscriminator<string>("AgentType")
                .HasValue<StudentAgent>("Student")
                .HasValue<OfficeWorkerAgent>("OfficeWorker")
                .HasValue<MedicalWorkerAgent>("MedicalWorker")
                .HasValue<RetailWorkerAgent>("RetailWorker")
                .HasValue<ElderlyRetiredAgent>("ElderlyRetired")
                .HasValue<ServiceWorkerAgent>("ServiceWorker");

            // Configure Agent to RoutineEvent relationship
            modelBuilder.Entity<Agent>()
                .HasMany(a => a.RoutineEvents)
                .WithOne(e => e.Agent)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the conversion for CachedPath property as it stores a complex type (non primative)
            var jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true  // Enables serialization of tuple fields (Item1, Item2)
            };

            modelBuilder.Entity<RoutineEvent>()
                .Property(e => e.CachedPath)
                .HasConversion(
                    // Serialize the List to JSON with proper tuple support
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    // Deserialize JSON back to List
                    v => JsonSerializer.Deserialize<List<(double Lat, double Lon)>>(v, jsonOptions)
                        ?? new List<(double, double)>()
                )
                .HasColumnType("jsonb");

            // Configure one-to-one relationship between Agent and MedicalProfile
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.MedicalProfile)
                .WithOne(m => m.Agent)
                .HasForeignKey<MedicalProfile>(m => m.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Agent to Home relationship
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.House)
                .WithMany()
                .HasForeignKey(a => a.HouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Agent to Workplace relationship
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.Workplace)
                .WithMany()
                .HasForeignKey(a => a.WorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Simulation to MapArea one-to-one relationship
            modelBuilder.Entity<Simulation>()
                .HasOne(s => s.MapArea)
                .WithOne(m => m.Simulation)
                .HasForeignKey<MapArea>(m => m.SimulationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasMany(s => s.Agents)
                .WithOne(a => a.Simulation)
                .HasForeignKey(a => a.SimulationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasOne(s => s.Disease)
                .WithOne(d => d.Simulation)
                .HasForeignKey<Disease>(d => d.SimulationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasMany(s => s.Mitigations)
                .WithOne(m => m.Simulation)
                .HasForeignKey(m => m.SimulationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasMany(s => s.SimulationSnapshots)
                .WithOne(snap => snap.Simulation)
                .HasForeignKey("SimulationId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasMany(s => s.SimulationMetrics)
                .WithOne(metric => metric.Simulation)
                .HasForeignKey("SimulationId")
                .OnDelete(DeleteBehavior.Cascade);


            // SimulationMetric unique index
            modelBuilder.Entity<SimulationMetric>()
                .HasIndex(m => new { m.SimulationId, m.Tick })
                .IsUnique();

            // Configure MapArea cascade deletes to ensure related entities are deleted when a MapArea is removed reducing orphaned records and maintaining data integrity
            modelBuilder.Entity<MapArea>()
                .HasMany(m => m.Buildings)
                .WithOne(b => b.MapArea)
                .HasForeignKey(b => b.MapAreaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MapArea>()
                .HasMany(m => m.Paths)
                .WithOne(p => p.MapArea)
                .HasForeignKey(p => p.MapAreaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.Path>()
                .HasMany(p => p.Nodes)
                .WithOne(n => n.Path)
                .HasForeignKey(n => n.PathId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Building>()
                .HasDiscriminator<string>("BuildingType")
                .HasValue<ResidentialBuilding>("Residential")
                .HasValue<OfficeBuilding>("Office")
                .HasValue<EducationBuilding>("Education")
                .HasValue<MedicalBuilding>("Medical")
                .HasValue<RetailBuilding>("Retail")
                .HasValue<LeisureBuilding>("Leisure");

            modelBuilder.Entity<Mitigation>()
                .HasDiscriminator<string>("MitigationType")
                .HasValue<SelfIsolationMitigation>("SelfIsolation")
                .HasValue<MaskWearingMitigation>("MaskWearing")
                .HasValue<VaccinationMitigation>("Vaccination");

            //Configure Preset cascade deletes
            modelBuilder.Entity<MapAreaPreset>()
                .HasMany(m => m.Buildings)
                .WithOne(b => b.MapAreaPreset)
                .HasForeignKey(b => b.MapAreaPresetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MapAreaPreset>()
                .HasMany(m => m.Paths)
                .WithOne(p => p.MapAreaPreset)
                .HasForeignKey(p => p.MapAreaPresetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OSMPathPreset>()
                .HasMany(p => p.Nodes)
                .WithOne(n => n.OSMPathPreset)
                .HasForeignKey(n => n.OSMPathPresetId)
                .OnDelete(DeleteBehavior.Cascade);

            // configure SimulationSnapshot relationships
            modelBuilder.Entity<SimulationSnapshot>()
                .HasMany(s => s.Agents)
                .WithOne(a => a.Snapshot)
                .HasForeignKey(a => a.SimulationSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SimulationSnapshot>()
                .HasMany(s => s.Buildings)
                .WithOne(b => b.Snapshot)
                .HasForeignKey(b => b.SimulationSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            // SimulationSnapshot unique index
            modelBuilder.Entity<SimulationSnapshot>()
                .HasIndex(s => new { s.SimulationId, s.Tick })
                .IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseNpgsql("Host=localhost;Database=simdb;Username=simuser;Password=simpassword");
            }
        }
    }
}