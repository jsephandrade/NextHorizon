using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
using NextHorizon.Models.Agent;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;
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

    public DbSet<SupportContactChannel> SupportContactChannels => Set<SupportContactChannel>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<FaqRecord> FaqRecords => Set<FaqRecord>();

    public DbSet<SupportFaqRecord> SupportFaqRecords => Set<SupportFaqRecord>();

    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    public DbSet<LiveAgentSession> LiveAgentSessions => Set<LiveAgentSession>();

    public DbSet<SupportAgentRecord> SupportAgents => Set<SupportAgentRecord>();

    public DbSet<QaReview> QaReviews => Set<QaReview>();

    public DbSet<QaReviewQuestionScore> QaReviewQuestionScores => Set<QaReviewQuestionScore>();

    public DbSet<QaReviewInlineCommentDraft> QaReviewInlineCommentDrafts => Set<QaReviewInlineCommentDraft>();

    public DbSet<AgentReviewFeedback> AgentReviewFeedbackEntries => Set<AgentReviewFeedback>();

    public DbSet<AgentRanking> AgentRankings => Set<AgentRanking>();

    public DbSet<AgentNotificationRecord> Notifications => Set<AgentNotificationRecord>();

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

        var supportAgent = builder.Entity<SupportAgentRecord>();
        supportAgent.ToTable("Agents", "dbo", table => table.ExcludeFromMigrations());
        supportAgent.HasKey(x => x.ChatId);
        supportAgent.Property(x => x.ChatId)
            .HasColumnName("ChatID")
            .ValueGeneratedNever();
        supportAgent.Property(x => x.ConversationId)
            .HasColumnName("ConversationID");
        supportAgent.Property(x => x.AgentName)
            .HasColumnName("AgentName");
        supportAgent.Property(x => x.ClientName)
            .HasColumnName("ClientName");
        supportAgent.Property(x => x.Category)
            .HasColumnName("Category");
        supportAgent.Property(x => x.PreviewQuestion)
            .HasColumnName("PreviewQuestion");
        supportAgent.Property(x => x.ChatStatus)
            .HasColumnName("ChatStatus");
        supportAgent.Property(x => x.AgentStatus)
            .HasColumnName("AgentStatus");
        supportAgent.Property(x => x.AgentId)
            .HasColumnName("AgentID");
        supportAgent.Property(x => x.UserId)
            .HasColumnName("UserID")
            .IsRequired();
        supportAgent.Property(x => x.ChatSlot)
            .HasColumnName("ChatSlot");
        supportAgent.Property(x => x.Notes)
            .HasColumnName("Notes");
        supportAgent.Property(x => x.NotesLastUpdatedAt)
            .HasColumnName("NotesLastUpdatedAt")
            .HasColumnType("datetime2");
        supportAgent.Property(x => x.ACWStartTime)
            .HasColumnName("ACWStartTime")
            .HasColumnType("datetime2");
        supportAgent.Property(x => x.ACWEndTime)
            .HasColumnName("ACWEndTime")
            .HasColumnType("datetime2");

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
        supportFaqRecord.Property(x => x.Status)
            .HasColumnName("Status")
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
        supportFaqRecord.Property(x => x.EndTime)
            .HasColumnName("EndTime")
            .HasColumnType("datetime2");
        supportFaqRecord.Property(x => x.StartTime)
            .HasColumnName("StartTime")
            .HasColumnType("datetime2");

        var supportMessage = builder.Entity<SupportMessage>();
        supportMessage.ToTable("SupportMessages", "dbo", table => table.ExcludeFromMigrations());
        supportMessage.HasKey(x => x.Id);
        supportMessage.Property(x => x.Id)
            .HasColumnName("Id");
        supportMessage.Property(x => x.ConversationId)
            .HasColumnName("ConversationId")
            .IsRequired();
        supportMessage.Property(x => x.SenderId)
            .HasColumnName("SenderId")
            .IsRequired();
        supportMessage.Property(x => x.SenderRole)
            .HasColumnName("SenderRole")
            .IsRequired();
        supportMessage.Property(x => x.MessageText)
            .HasColumnName("MessageText")
            .IsRequired();
        supportMessage.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .IsRequired();

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
        liveAgentSession.Property(x => x.EndedReason)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(LiveAgentSessionEndedReason.None);
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
        liveAgentSession.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter($"[Status] <> {(byte)LiveAgentSessionStatus.Resolved}");
        liveAgentSession.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt })
            .IsDescending(false, false, true);

        var qaReview = builder.Entity<QaReview>();
        qaReview.ToTable("QaReviews");
        qaReview.HasKey(x => x.QaReviewId);
        qaReview.Property(x => x.ReviewerName)
            .IsRequired()
            .HasMaxLength(200);
        qaReview.Property(x => x.Notes)
            .HasMaxLength(4000);
        qaReview.Property(x => x.AccuracyAverage)
            .HasColumnType("decimal(5,2)");
        qaReview.Property(x => x.ToneAverage)
            .HasColumnType("decimal(5,2)");
        qaReview.Property(x => x.ResolutionAverage)
            .HasColumnType("decimal(5,2)");
        qaReview.Property(x => x.OverallPercent)
            .HasColumnType("decimal(5,2)");
        qaReview.Property(x => x.InlineCommentsJson)
            .HasDefaultValue("{}");
        qaReview.Property(x => x.CreatedAtUtc)
            .IsRequired();
        qaReview.Property(x => x.UpdatedAtUtc)
            .IsRequired();
        qaReview.HasIndex(x => x.SupportFaqId)
            .IsUnique();

        var qaReviewInlineCommentDraft = builder.Entity<QaReviewInlineCommentDraft>();
        qaReviewInlineCommentDraft.ToTable("QaReviewInlineCommentDrafts");
        qaReviewInlineCommentDraft.HasKey(x => x.QaReviewInlineCommentDraftId);
        qaReviewInlineCommentDraft.Property(x => x.UpdatedByName)
            .IsRequired()
            .HasMaxLength(200);
        qaReviewInlineCommentDraft.Property(x => x.InlineCommentsJson)
            .IsRequired()
            .HasDefaultValue("{}");
        qaReviewInlineCommentDraft.Property(x => x.CreatedAtUtc)
            .IsRequired();
        qaReviewInlineCommentDraft.Property(x => x.UpdatedAtUtc)
            .IsRequired();
        qaReviewInlineCommentDraft.HasIndex(x => x.SupportFaqId)
            .IsUnique();

        var agentReviewFeedback = builder.Entity<AgentReviewFeedback>();
        agentReviewFeedback.ToTable("AgentReviewFeedback");
        agentReviewFeedback.HasKey(x => x.AgentReviewFeedbackId);
        agentReviewFeedback.Property(x => x.Notes)
            .IsRequired()
            .HasMaxLength(4000)
            .HasDefaultValue(string.Empty);
        agentReviewFeedback.Property(x => x.Acknowledged)
            .IsRequired()
            .HasDefaultValue(false);
        agentReviewFeedback.Property(x => x.AcknowledgedAtUtc);
        agentReviewFeedback.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        agentReviewFeedback.Property(x => x.UpdatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        agentReviewFeedback.HasIndex(x => new { x.SupportFaqId, x.AgentUserId })
            .IsUnique();

        var agentRanking = builder.Entity<AgentRanking>();
        agentRanking.ToTable("AgentRankings", "dbo");
        agentRanking.HasKey(x => x.AgentRankingId);
        agentRanking.Property(x => x.AgentUserId)
            .IsRequired();
        agentRanking.Property(x => x.PeriodStartUtc)
            .IsRequired()
            .HasColumnType("datetime2");
        agentRanking.Property(x => x.MetricType)
            .IsRequired()
            .HasConversion<byte>();
        agentRanking.Property(x => x.MetricValue)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
        agentRanking.Property(x => x.ReviewCount)
            .IsRequired();
        agentRanking.Property(x => x.RankPosition)
            .IsRequired();
        agentRanking.Property(x => x.RankedAgentCount)
            .IsRequired();
        agentRanking.Property(x => x.CalculatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        agentRanking.Property(x => x.UpdatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        agentRanking.HasIndex(x => new { x.PeriodStartUtc, x.MetricType, x.AgentUserId })
            .IsUnique();
        agentRanking.HasIndex(x => new { x.PeriodStartUtc, x.MetricType, x.RankPosition });

        var notification = builder.Entity<AgentNotificationRecord>();
        notification.ToTable("Notifications", "dbo", table => table.ExcludeFromMigrations());
        notification.HasKey(x => x.NotificationId);
        notification.Property(x => x.NotificationId)
            .HasColumnName("NotificationId");
        notification.Property(x => x.RecipientType)
            .HasColumnName("RecipientType")
            .IsRequired();
        notification.Property(x => x.RecipientId)
            .HasColumnName("RecipientId")
            .IsRequired();
        notification.Property(x => x.OrderId)
            .HasColumnName("OrderId");
        notification.Property(x => x.Message)
            .HasColumnName("Message")
            .IsRequired();
        notification.Property(x => x.IsRead)
            .HasColumnName("IsRead")
            .IsRequired();
        notification.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .IsRequired();
        notification.Property(x => x.Category)
            .HasColumnName("category")
            .IsRequired();

        var qaReviewQuestionScore = builder.Entity<QaReviewQuestionScore>();
        qaReviewQuestionScore.ToTable("QaReviewQuestionScores");
        qaReviewQuestionScore.HasKey(x => x.QaReviewQuestionScoreId);
        qaReviewQuestionScore.Property(x => x.QuestionKey)
            .IsRequired()
            .HasMaxLength(40);
        qaReviewQuestionScore.Property(x => x.Score)
            .IsRequired();
        qaReviewQuestionScore.HasOne(x => x.Review)
            .WithMany(x => x.QuestionScores)
            .HasForeignKey(x => x.QaReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        qaReviewQuestionScore.HasIndex(x => new { x.QaReviewId, x.QuestionKey })
            .IsUnique();

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


