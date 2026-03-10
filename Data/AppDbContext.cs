using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Models;

namespace MyAspNetApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DbProduct> Products { get; set; }
        public DbSet<DbProductColorImage> ProductColorImages { get; set; }
        public DbSet<DbOrder> Orders { get; set; }
        public DbSet<DbOrderItem> OrderItems { get; set; }
        public DbSet<DbConsumer> Consumers { get; set; }
        public DbSet<DbUser> Users { get; set; }
    }
}
