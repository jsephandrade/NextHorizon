using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Models;

namespace MyAspNetApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DbProduct> Products { get; set; }
        public DbSet<DbProductColorImage> ProductColorImages { get; set; }
        public DbSet<DbReview> Reviews { get; set; }
        public DbSet<DbReviewImage> ReviewImages { get; set; }
    }
}
