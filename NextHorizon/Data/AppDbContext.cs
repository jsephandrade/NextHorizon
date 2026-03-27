using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
using NextHorizon.Messaging.Models;

namespace NextHorizon.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Existing DbSets
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Consumer> Consumers => Set<Consumer>();
    public DbSet<SellerAccount> SellerAccounts => Set<SellerAccount>();
    public DbSet<MessageConversation> MessageConversations => Set<MessageConversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Logistics> Logistics => Set<Logistics>(); // Fixed this line to match the rest!
public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Removed the temporary warning suppressor so your app is clean for production!
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  
        
        // ============== DECIMAL & TRIGGER FIXES ==============
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("SomeTriggerName")); 
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.ShippingFee).HasColumnType("decimal(18,2)");
            entity.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<OrderItem>()
            .Property(o => o.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        // ============== EXISTING CONFIGURATIONS ==============
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

        // ============== REFERENCE ENTITIES (EXCLUDED FROM MIGRATIONS) ==============
        
        var platformUser = modelBuilder.Entity<PlatformUser>();
        platformUser.ToTable("Users", "dbo", table => table.ExcludeFromMigrations());
        platformUser.HasKey(x => x.UserId);
        platformUser.Property(x => x.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();
        platformUser.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        var consumerRef = modelBuilder.Entity<ConsumerRef>();
        consumerRef.ToTable("Consumers", "dbo", table => table.ExcludeFromMigrations());
        consumerRef.HasKey(x => x.ConsumerId);
        consumerRef.Property(x => x.ConsumerId)
            .HasColumnName("consumer_id")
            .ValueGeneratedNever();
        consumerRef.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        consumerRef.Property(x => x.FirstName)
            .HasColumnName("first_name");
        consumerRef.Property(x => x.MiddleName)
            .HasColumnName("middle_name");
        consumerRef.Property(x => x.LastName)
            .HasColumnName("last_name");
        consumerRef.Property(x => x.Username)
            .HasColumnName("username");

        var sellerRef = modelBuilder.Entity<SellerRef>();
        sellerRef.ToTable("Sellers", "dbo", table => table.ExcludeFromMigrations());
        sellerRef.HasKey(x => x.SellerId);
        sellerRef.Property(x => x.SellerId)
            .HasColumnName("seller_id")
            .ValueGeneratedNever();
        sellerRef.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();


        // ============== MESSAGING CONVERSATIONS ==============
        
        modelBuilder.Entity<MessageConversation>(entity =>
        {
            entity.ToTable("MessagingConversations", table =>
            {
                table.HasCheckConstraint("CK_MessagingConversations_ContextType", "[ContextType] IN (1, 2)");
                table.HasCheckConstraint(
                    "CK_MessagingConversations_ContextType_Order",
                    "([ContextType] = 1 AND [OrderId] IS NULL) OR ([ContextType] = 2 AND [OrderId] IS NOT NULL)");
            });

            entity.HasKey(x => x.ConversationId);

            entity.Property(x => x.BuyerUserId)
                .IsRequired();

            entity.Property(x => x.SellerUserId)
                .IsRequired();

            entity.Property(x => x.ContextType)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(x => x.OrderId);

            entity.Property(x => x.LastMessageAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.BuyerLastReadAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.SellerLastReadAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(x => x.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne<ConsumerRef>()
                .WithMany()
                .HasForeignKey(x => x.BuyerUserId)
                .HasPrincipalKey(x => x.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<SellerRef>()
                .WithMany()
                .HasForeignKey(x => x.SellerUserId)
                .HasPrincipalKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.BuyerUserId, x.SellerUserId, x.ContextType })
                .IsUnique()
                .HasFilter("[ContextType] = 1");

            entity.HasIndex(x => new { x.OrderId, x.ContextType })
                .IsUnique()
                .HasFilter("[ContextType] = 2");

            entity.HasIndex(x => x.BuyerUserId);
            entity.HasIndex(x => x.SellerUserId);
            entity.HasIndex(x => x.LastMessageAt);
        });

        // ============== CONVERSATION MESSAGES ==============
        
        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.ToTable("MessagingMessages");
            entity.HasKey(x => x.MessageId);

            entity.Property(x => x.SenderUserId)
                .IsRequired();

            entity.Property(x => x.Body)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.AttachmentUrl)
                .HasMaxLength(400);

            entity.Property(x => x.SentAt)
                .IsRequired()
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .HasPrincipalKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ConversationId, x.SentAt })
                .IsDescending(false, true);
        });
    }
}