using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Models;

namespace MySimulationAPI.Data
{
    public class SimDbContext : DbContext
    {
        public SimDbContext(DbContextOptions<SimDbContext> options) : base(options) { }

        // list of models3 to create tables for
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Simulation> Simulations { get; set; } = null!;
        public DbSet<Agent> Agents { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseNpgsql("Host=localhost;Database=simdb;Username=simuser;Password=simpassword");
            }
        }
    }
}