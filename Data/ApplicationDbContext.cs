using Microsoft.EntityFrameworkCore;
using SqlAPI.Models;

namespace SqlAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Person> People { get; set; }
    }
}
