using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Models;

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
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<Models.Path> Paths { get; set; } = null!;
        public DbSet<Disease> Diseases { get; set; } = null!;
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
                .HasDiscriminator<string>("AgentType") // discriminator column
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

            // Configure one-to-one relationship between Agent and MedicalProfile
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.MedicalProfile)
                .WithOne(m => m.Agent)
                .HasForeignKey<MedicalProfile>(m => m.AgentId);

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