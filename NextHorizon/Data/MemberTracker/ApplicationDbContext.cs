using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
using NextHorizon.Messaging.Models;
using NextHorizon.Modules.MemberTracker.Models;

namespace NextHorizon.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MemberUpload> MemberUploads => Set<MemberUpload>();

    public DbSet<MessageConversation> MessageConversations => Set<MessageConversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var platformUser = builder.Entity<PlatformUser>();
        platformUser.ToTable("Users", "dbo", table => table.ExcludeFromMigrations());
        platformUser.HasKey(x => x.UserId);
        platformUser.Property(x => x.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();
        platformUser.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        var consumer = builder.Entity<ConsumerRef>();
        consumer.ToTable("Consumers", "dbo", table => table.ExcludeFromMigrations());
        consumer.HasKey(x => x.ConsumerId);
        consumer.Property(x => x.ConsumerId)
            .HasColumnName("consumer_id")
            .ValueGeneratedNever();
        consumer.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        var seller = builder.Entity<SellerRef>();
        seller.ToTable("Sellers", "dbo", table => table.ExcludeFromMigrations());
        seller.HasKey(x => x.SellerId);
        seller.Property(x => x.SellerId)
            .HasColumnName("seller_id")
            .ValueGeneratedNever();
        seller.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        var customer = builder.Entity<Customer>();
        customer.ToTable("Customers", "dbo", table => table.ExcludeFromMigrations());
        customer.HasKey(x => x.Id);
        customer.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();
        customer.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();
        customer.HasIndex(x => x.Email)
            .IsUnique();
        customer.Property(x => x.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        var upload = builder.Entity<MemberUpload>();

        upload.ToTable("MemberUploads");
        upload.HasKey(x => x.UploadId);

        upload.Property(x => x.UserId)
            .IsRequired();

        upload.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(100);

        upload.Property(x => x.ActivityName)
            .IsRequired()
            .HasMaxLength(80);

        upload.Property(x => x.ActivityDate)
            .IsRequired()
            .HasColumnType("date");

        upload.Property(x => x.ProofUrl)
            .IsRequired()
            .HasMaxLength(400);

        upload.Property(x => x.DistanceKm)
            .IsRequired()
            .HasPrecision(6, 2);

        upload.Property(x => x.MovingTimeSec)
            .IsRequired();

        upload.Property(x => x.Steps);

        upload.Property(x => x.AvgPaceSecPerKm);

        upload.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        upload.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        upload.HasOne<ConsumerRef>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);

        upload.HasIndex(x => new { x.UserId, x.CreatedAt })
            .IsDescending(false, true);

        upload.HasIndex(x => x.ActivityDate)
            .IsDescending(true);

        var conversation = builder.Entity<MessageConversation>();

        conversation.ToTable("MessagingConversations", table =>
        {
            table.HasCheckConstraint("CK_MessagingConversations_ContextType", "[ContextType] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_MessagingConversations_ContextType_Order",
                "([ContextType] = 1 AND [OrderId] IS NULL) OR ([ContextType] = 2 AND [OrderId] IS NOT NULL)");
        });

        conversation.HasKey(x => x.ConversationId);

        conversation.Property(x => x.BuyerUserId)
            .IsRequired();

        conversation.Property(x => x.SellerUserId)
            .IsRequired();

        conversation.Property(x => x.ContextType)
            .IsRequired()
            .HasConversion<byte>();

        conversation.Property(x => x.OrderId);

        conversation.Property(x => x.LastMessageAt)
            .HasColumnType("datetime2");

        conversation.Property(x => x.BuyerLastReadAt)
            .HasColumnType("datetime2");

        conversation.Property(x => x.SellerLastReadAt)
            .HasColumnType("datetime2");

        conversation.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        conversation.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        conversation.HasOne<ConsumerRef>()
            .WithMany()
            .HasForeignKey(x => x.BuyerUserId)
            .HasPrincipalKey(x => x.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);

        conversation.HasOne<SellerRef>()
            .WithMany()
            .HasForeignKey(x => x.SellerUserId)
            .HasPrincipalKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        conversation.HasIndex(x => new { x.BuyerUserId, x.SellerUserId, x.ContextType })
            .IsUnique()
            .HasFilter("[ContextType] = 1");

        conversation.HasIndex(x => new { x.OrderId, x.ContextType })
            .IsUnique()
            .HasFilter("[ContextType] = 2");

        conversation.HasIndex(x => x.BuyerUserId);
        conversation.HasIndex(x => x.SellerUserId);
        conversation.HasIndex(x => x.LastMessageAt);

        var message = builder.Entity<ConversationMessage>();

        message.ToTable("MessagingMessages");
        message.HasKey(x => x.MessageId);

        message.Property(x => x.SenderUserId)
            .IsRequired();

        message.Property(x => x.Body)
            .IsRequired()
            .HasMaxLength(2000);

        message.Property(x => x.AttachmentUrl)
            .HasMaxLength(400);

        message.Property(x => x.SentAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        message.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        message.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        message.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        message.HasIndex(x => new { x.ConversationId, x.SentAt })
            .IsDescending(false, true);
    }
}

