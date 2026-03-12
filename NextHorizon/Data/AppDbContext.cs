using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;

namespace NextHorizon.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Consumer> Consumers => Set<Consumer>();
    public DbSet<SellerAccount> SellerAccounts => Set<SellerAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.UserType).HasColumnName("user_type");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Consumer>(entity =>
        {
            entity.ToTable("Consumers");
            entity.HasKey(e => e.ConsumerId);
            entity.Property(e => e.ConsumerId).HasColumnName("consumer_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<SellerAccount>(entity =>
        {
            entity.ToTable("Sellers");
            entity.HasKey(e => e.SellerId);
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BusinessType).HasColumnName("business_type");
            entity.Property(e => e.BusinessName).HasColumnName("business_name");
            entity.Property(e => e.BusinessEmail).HasColumnName("business_email");
            entity.Property(e => e.BusinessPhone).HasColumnName("business_phone");
            entity.Property(e => e.TaxId).HasColumnName("tax_id");
            entity.Property(e => e.BusinessAddress).HasColumnName("business_address");
            entity.Property(e => e.LogoPath).HasColumnName("logo_path");
            entity.Property(e => e.DocumentPath).HasColumnName("document_path");
            entity.Property(e => e.SellerStatus).HasColumnName("seller_status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
