using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
using NextHorizon.Models.HelpCenter;
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

    public DbSet<HelpCategory> HelpCategories => Set<HelpCategory>();

    public DbSet<HelpFaq> HelpFaqs => Set<HelpFaq>();

    public DbSet<SupportContactChannel> SupportContactChannels => Set<SupportContactChannel>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<FaqRecord> FaqRecords => Set<FaqRecord>();

    public DbSet<SupportFaqRecord> SupportFaqRecords => Set<SupportFaqRecord>();

    public DbSet<LiveAgentSession> LiveAgentSessions => Set<LiveAgentSession>();

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
        consumer.Property(x => x.FirstName)
            .HasColumnName("first_name");
        consumer.Property(x => x.MiddleName)
            .HasColumnName("middle_name");
        consumer.Property(x => x.LastName)
            .HasColumnName("last_name");
        consumer.Property(x => x.Username)
            .HasColumnName("username");

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
        customer.Property(x => x.ConsumerId);
        customer.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();
        customer.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();
        customer.HasIndex(x => x.ConsumerId)
            .IsUnique()
            .HasFilter("[ConsumerId] IS NOT NULL");
        customer.HasIndex(x => x.Email)
            .IsUnique();
        customer.Property(x => x.CreatedUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");
        customer.HasOne<ConsumerRef>()
            .WithMany()
            .HasForeignKey(x => x.ConsumerId)
            .HasPrincipalKey(x => x.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);

        var faqRecord = builder.Entity<FaqRecord>();
        faqRecord.ToTable("FAQs", "dbo", table => table.ExcludeFromMigrations());
        faqRecord.HasKey(x => x.FaqId);
        faqRecord.Property(x => x.FaqId)
            .HasColumnName("FaqID");
        faqRecord.Property(x => x.Question)
            .HasColumnName("Question")
            .IsRequired()
            .HasMaxLength(500);
        faqRecord.Property(x => x.Answer)
            .HasColumnName("Answer")
            .IsRequired()
            .HasMaxLength(4000);
        faqRecord.Property(x => x.Category)
            .HasColumnName("Category")
            .IsRequired()
            .HasMaxLength(120);
        faqRecord.Property(x => x.Status)
            .HasColumnName("Status")
            .IsRequired()
            .HasMaxLength(40);
        faqRecord.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        faqRecord.Property(x => x.DateAdded)
            .HasColumnName("DateAdded")
            .HasColumnType("datetime2");
        faqRecord.Property(x => x.LastUpdated)
            .HasColumnName("LastUpdated")
            .HasColumnType("datetime2");
        faqRecord.Property(x => x.UserType)
            .HasColumnName("UserType")
            .IsRequired()
            .HasMaxLength(40);

        var supportFaqRecord = builder.Entity<SupportFaqRecord>();
        supportFaqRecord.ToTable("SupportFAQs", "dbo", table => table.ExcludeFromMigrations());
        supportFaqRecord.HasKey(x => x.Id);
        supportFaqRecord.Property(x => x.Id)
            .HasColumnName("Id");
        supportFaqRecord.Property(x => x.Category)
            .HasColumnName("Category")
            .IsRequired();
        supportFaqRecord.Property(x => x.Question)
            .HasColumnName("Question")
            .IsRequired();
        supportFaqRecord.Property(x => x.Resolution)
            .HasColumnName("Resolution")
            .IsRequired();
        supportFaqRecord.Property(x => x.DurationMinutes)
            .HasColumnName("DurationMinutes")
            .IsRequired();
        supportFaqRecord.Property(x => x.UserType)
            .HasColumnName("UserType")
            .IsRequired();
        supportFaqRecord.Property(x => x.AgentId)
            .HasColumnName("AgentId");
        supportFaqRecord.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .IsRequired();
        var helpCategory = builder.Entity<HelpCategory>();
        helpCategory.ToTable("HelpCategories");
        helpCategory.HasKey(x => x.HelpCategoryId);
        helpCategory.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(80);
        helpCategory.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(120);
        helpCategory.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);
        helpCategory.Property(x => x.IconKey)
            .IsRequired()
            .HasMaxLength(80);
        helpCategory.Property(x => x.IsActive)
            .HasDefaultValue(true);
        helpCategory.HasIndex(x => x.Slug)
            .IsUnique();
        helpCategory.HasIndex(x => new { x.IsActive, x.DisplayOrder });

        var helpFaq = builder.Entity<HelpFaq>();
        helpFaq.ToTable("HelpFaqs");
        helpFaq.HasKey(x => x.HelpFaqId);
        helpFaq.Property(x => x.Question)
            .IsRequired()
            .HasMaxLength(200);
        helpFaq.Property(x => x.Answer)
            .IsRequired()
            .HasMaxLength(4000);
        helpFaq.Property(x => x.SearchKeywords)
            .HasMaxLength(400);
        helpFaq.Property(x => x.IsActive)
            .HasDefaultValue(true);
        helpFaq.HasOne(x => x.Category)
            .WithMany(x => x.Faqs)
            .HasForeignKey(x => x.HelpCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        helpFaq.HasIndex(x => new { x.HelpCategoryId, x.IsActive, x.DisplayOrder });
        helpFaq.HasIndex(x => x.IsFeaturedOnHome);

        var supportContactChannel = builder.Entity<SupportContactChannel>();
        supportContactChannel.ToTable("SupportContactChannels");
        supportContactChannel.HasKey(x => x.SupportContactChannelId);
        supportContactChannel.Property(x => x.ChannelType)
            .IsRequired()
            .HasMaxLength(40);
        supportContactChannel.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(80);
        supportContactChannel.Property(x => x.Value)
            .IsRequired()
            .HasMaxLength(320);
        supportContactChannel.Property(x => x.DisplayText)
            .IsRequired()
            .HasMaxLength(320);
        supportContactChannel.Property(x => x.ActionHref)
            .IsRequired()
            .HasMaxLength(400);
        supportContactChannel.Property(x => x.IsActive)
            .HasDefaultValue(true);
        supportContactChannel.HasIndex(x => new { x.IsActive, x.DisplayOrder });

        var supportTicket = builder.Entity<SupportTicket>();
        supportTicket.ToTable("SupportTickets");
        supportTicket.HasKey(x => x.SupportTicketId);
        supportTicket.Property(x => x.ReferenceCode)
            .IsRequired()
            .HasMaxLength(40);
        supportTicket.Property(x => x.FaqCategory)
            .HasMaxLength(120);
        supportTicket.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(160);
        supportTicket.Property(x => x.Body)
            .IsRequired()
            .HasMaxLength(4000);
        supportTicket.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();
        supportTicket.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        supportTicket.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        supportTicket.HasOne(x => x.Category)
            .WithMany(x => x.SupportTickets)
            .HasForeignKey(x => x.HelpCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        supportTicket.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        supportTicket.HasOne<ConsumerRef>()
            .WithMany()
            .HasForeignKey(x => x.ConsumerId)
            .HasPrincipalKey(x => x.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);
        supportTicket.HasIndex(x => x.ReferenceCode)
            .IsUnique();
        supportTicket.HasIndex(x => new { x.UserId, x.CreatedAt })
            .IsDescending(false, true);
        supportTicket.HasIndex(x => new { x.Status, x.CreatedAt })
            .IsDescending(false, true);

        var liveAgentSession = builder.Entity<LiveAgentSession>();
        liveAgentSession.ToTable("LiveAgentSessions");
        liveAgentSession.HasKey(x => x.LiveAgentSessionId);
        liveAgentSession.Property(x => x.CategorySlug)
            .IsRequired()
            .HasMaxLength(80);
        liveAgentSession.Property(x => x.CategoryTitle)
            .IsRequired()
            .HasMaxLength(120);
        liveAgentSession.Property(x => x.FirstQuestion)
            .IsRequired()
            .HasMaxLength(4000)
            .HasDefaultValue(string.Empty);
        liveAgentSession.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>();
        liveAgentSession.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        liveAgentSession.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        liveAgentSession.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        liveAgentSession.HasOne<ConsumerRef>()
            .WithMany()
            .HasForeignKey(x => x.ConsumerId)
            .HasPrincipalKey(x => x.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);
        liveAgentSession.HasIndex(x => x.SupportFaqId)
            .IsUnique();
        liveAgentSession.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt })
            .IsDescending(false, false, true);

        supportContactChannel.HasData(HelpCenterSeed.ContactChannels);

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



