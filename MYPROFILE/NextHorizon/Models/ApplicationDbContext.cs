using Microsoft.EntityFrameworkCore;
using NextHorizon.Models.Dtos;

namespace NextHorizon.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<ProfileHeaderRow> ProfileHeaderRows => Set<ProfileHeaderRow>();
        public DbSet<ProfileTodayStatsRow> ProfileTodayStatsRows => Set<ProfileTodayStatsRow>();
        public DbSet<ProfileRecentUploadRow> ProfileRecentUploadRows => Set<ProfileRecentUploadRow>();
        public DbSet<User> Users { get; set; }
        public DbSet<Consumer> Consumers { get; set; }
        
        // New DbSets for fitness overview
        public DbSet<MemberUpload> MemberUploads { get; set; }
        public DbSet<DailyFitnessDataRow> DailyFitnessDataRows => Set<DailyFitnessDataRow>();
        public DbSet<DailyTotalsRow> DailyTotalsRows => Set<DailyTotalsRow>();
        public DbSet<WeeklyFitnessDataRow> WeeklyFitnessDataRows => Set<WeeklyFitnessDataRow>();
        public DbSet<WeeklyAveragesRow> WeeklyAveragesRows => Set<WeeklyAveragesRow>();
        public DbSet<MonthlyFitnessDataRow> MonthlyFitnessDataRows => Set<MonthlyFitnessDataRow>();
        public DbSet<MonthlyAveragesRow> MonthlyAveragesRows => Set<MonthlyAveragesRow>();
        public DbSet<YearlyFitnessDataRow> YearlyFitnessDataRows => Set<YearlyFitnessDataRow>();
        public DbSet<YearlyAveragesRow> YearlyAveragesRows => Set<YearlyAveragesRow>();
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<PayoutAccount> PayoutAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing keyless entities
            modelBuilder.Entity<ProfileHeaderRow>().HasNoKey();
            modelBuilder.Entity<ProfileTodayStatsRow>().HasNoKey();
            modelBuilder.Entity<ProfileRecentUploadRow>().HasNoKey();
            
            // New keyless entities for fitness overview
            modelBuilder.Entity<DailyFitnessDataRow>().HasNoKey();
            modelBuilder.Entity<DailyTotalsRow>().HasNoKey();
            modelBuilder.Entity<WeeklyFitnessDataRow>().HasNoKey();
            modelBuilder.Entity<WeeklyAveragesRow>().HasNoKey();
            modelBuilder.Entity<MonthlyFitnessDataRow>().HasNoKey();
            modelBuilder.Entity<MonthlyAveragesRow>().HasNoKey();
            modelBuilder.Entity<YearlyFitnessDataRow>().HasNoKey();
            modelBuilder.Entity<YearlyAveragesRow>().HasNoKey();
            
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                // Configure one-to-one relationship with Consumer
                entity.HasOne(u => u.Consumer)
                    .WithOne(c => c.User)
                    .HasForeignKey<Consumer>(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Consumer entity
            modelBuilder.Entity<Consumer>(entity =>
            {
                entity.ToTable("Consumers");
                entity.HasKey(e => e.ConsumerId);

                entity.Property(e => e.ConsumerId).HasColumnName("consumer_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.MiddleName).HasColumnName("middle_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.Address).HasColumnName("address");
                entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.Username).HasColumnName("username");

                // Create unique index on UserId to ensure one-to-one relationship
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // Configure MemberUpload entity
            modelBuilder.Entity<MemberUpload>(entity =>
            {
                entity.ToTable("MemberUploads");
                entity.HasKey(e => e.UploadId);

                entity.Property(e => e.UploadId).HasColumnName("UploadId");
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.Title).HasColumnName("Title");
                entity.Property(e => e.ActivityName).HasColumnName("ActivityName");
                entity.Property(e => e.ActivityDate).HasColumnName("ActivityDate");
                entity.Property(e => e.ProofUrl).HasColumnName("ProofUrl");
                entity.Property(e => e.DistanceKm).HasColumnName("DistanceKm");
                entity.Property(e => e.MovingTimeSec).HasColumnName("MovingTimeSec");
                entity.Property(e => e.AvgPaceSecPerKm).HasColumnName("AvgPaceSecPerKm");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
                entity.Property(e => e.Steps).HasColumnName("Steps");

                // Create index on UserId and ActivityDate for faster queries
                entity.HasIndex(e => new { e.UserId, e.ActivityDate }).HasDatabaseName("IX_MemberUploads_UserId_ActivityDate");
            });

             modelBuilder.Entity<ShippingAddress>(entity =>
            {
                entity.ToTable("ShippingAddresses");
                entity.HasKey(e => e.ShippingAddressId);

                entity.Property(e => e.ShippingAddressId).HasColumnName("shipping_address_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Region).HasColumnName("region").HasMaxLength(100);
                entity.Property(e => e.Province).HasColumnName("province").HasMaxLength(100);
                entity.Property(e => e.CityMunicipality).HasColumnName("city_municipality").HasMaxLength(100);
                entity.Property(e => e.Barangay).HasColumnName("barangay").HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(10);
                entity.Property(e => e.HouseNumber).HasColumnName("house_number").HasMaxLength(50);
                entity.Property(e => e.Building).HasColumnName("building").HasMaxLength(100);
                entity.Property(e => e.StreetName).HasColumnName("street_name").HasMaxLength(200);
                entity.Property(e => e.IsDefault).HasColumnName("is_default");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                // Ensure only one default address per user
                entity.HasIndex(e => new { e.UserId, e.IsDefault })
                    .HasDatabaseName("IX_ShippingAddresses_UserDefault")
                    .IsUnique()
                    .HasFilter("is_default = 1");

                // Foreign key relationship
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}