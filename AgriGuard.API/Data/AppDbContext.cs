using AgriGuard.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Data
{
    // Main database context for AgriGuard entities
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Database tables
        public DbSet<User> Users { get; set; }
        public DbSet<Crop> Crops { get; set; }
        public DbSet<UserPlant> UserPlants { get; set; }
        public DbSet<CareTaskTemplate> CareTaskTemplates { get; set; }
        public DbSet<CareTask> CareTasks { get; set; }
        public DbSet<Diagnosis> Diagnoses { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Governorate> Governorates { get; set; }

        // Configure database relationships and constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure comment relationships
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure like relationships
            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure country and governorate relationship
            modelBuilder.Entity<Country>()
                .HasMany(c => c.Governorates)
                .WithOne(g => g.Country)
                .HasForeignKey(g => g.CountryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate country names
            modelBuilder.Entity<Country>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Prevent duplicate governorates within the same country
            modelBuilder.Entity<Governorate>()
                .HasIndex(g => new { g.Name, g.CountryId })
                .IsUnique();
        }
    }
}