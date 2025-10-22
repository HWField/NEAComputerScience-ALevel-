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
        public DbSet<SimulationSnapshot> SimulationSnapshots { get; set; } = null!;
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

            // Configure Agent -> RoutineEvent relationship
            modelBuilder.Entity<Agent>()
                .HasMany(a => a.RoutineEvents)
                .WithOne(e => e.Agent)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Need to configure the conversion for CachedPath property as it stores a complex type (non primative)
            modelBuilder.Entity<RoutineEvent>()
                .Property(e => e.CachedPath)
                .HasConversion(
                    // Serialize the tuple array to JSON
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    // Deserialize JSON back to tuple array
                    v => JsonSerializer.Deserialize<(double Lat, double Lon)[]>(v, (JsonSerializerOptions?)null)
                        ?? Array.Empty<(double, double)>()
                )
                .HasColumnType("jsonb");

            // Configure one-to-one relationship between Agent and MedicalProfile
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.MedicalProfile)
                .WithOne(m => m.Agent)
                .HasForeignKey<MedicalProfile>(m => m.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Agent -> Home relationship
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.House)
                .WithMany()
                .HasForeignKey(a => a.HouseId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Agent -> Workplace relationship
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.Workplace)
                .WithMany()
                .HasForeignKey(a => a.WorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Simulation -> MapArea one-to-one relationship
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
                .WithOne()
                .HasForeignKey("SimulationId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasMany(s => s.SimulationSnapshots)
                .WithOne()
                .HasForeignKey("SimulationId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MapArea cascade deletes
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
                .HasValue<MaskWearing>("MaskWearing")
                .HasValue<SocialDistancing>("SocialDistancing");

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