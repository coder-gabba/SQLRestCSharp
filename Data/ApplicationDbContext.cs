using Microsoft.EntityFrameworkCore;
using SqlAPI.Models;

namespace SqlAPI.Data
{
    /// <summary>
    /// Database context for the SQL API application
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext
        /// </summary>
        /// <param name="options">Database context options</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }

        /// <summary>
        /// Gets or sets the People DbSet
        /// </summary>
        public DbSet<Person> People { get; set; } = null!;

        /// <summary>
        /// Gets or sets the Users DbSet
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Configure the model using the model builder
        /// </summary>
        /// <param name="modelBuilder">The model builder instance</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Person entity
            modelBuilder.Entity<Person>(entity =>
            {
                entity.HasIndex(p => p.Email).IsUnique();
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Email).IsRequired().HasMaxLength(100);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            });
        }
    }
}
