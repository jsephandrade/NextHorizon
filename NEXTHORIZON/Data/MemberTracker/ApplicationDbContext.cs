using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Models.Messaging;
using NextHorizon.Models.AgentDashboard;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SupportFaqRecord> SupportFaqRecords => Set<SupportFaqRecord>();

    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    public DbSet<LiveAgentSession> LiveAgentSessions => Set<LiveAgentSession>();

    public DbSet<MessageConversation> MessageConversations => Set<MessageConversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    public DbSet<SupportAgentRecord> SupportAgents => Set<SupportAgentRecord>();

    public DbSet<QaReview> QaReviews => Set<QaReview>();

    public DbSet<QaEvaluationTemplate> QaEvaluationTemplates => Set<QaEvaluationTemplate>();

    public DbSet<QaEvaluationCategory> QaEvaluationCategories => Set<QaEvaluationCategory>();

    public DbSet<QaEvaluationQuestion> QaEvaluationQuestions => Set<QaEvaluationQuestion>();

    public DbSet<QaReviewCategoryScore> QaReviewCategoryScores => Set<QaReviewCategoryScore>();

    public DbSet<QaReviewQuestionScore> QaReviewQuestionScores => Set<QaReviewQuestionScore>();

    public DbSet<QaReviewInlineCommentDraft> QaReviewInlineCommentDrafts => Set<QaReviewInlineCommentDraft>();

    public DbSet<AgentReviewFeedback> AgentReviewFeedbackEntries => Set<AgentReviewFeedback>();

    public DbSet<AgentRanking> AgentRankings => Set<AgentRanking>();

    public DbSet<AgentNotificationRecord> Notifications => Set<AgentNotificationRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

        var liveAgentSession = builder.Entity<LiveAgentSession>();
        liveAgentSession.ToTable("LiveAgentSessions", table => table.ExcludeFromMigrations());
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
        liveAgentSession.HasIndex(x => x.SupportFaqId)
            .IsUnique();
        liveAgentSession.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt })
            .IsDescending(false, false, true);

        var conversation = builder.Entity<MessageConversation>();
        conversation.ToTable("MessagingConversations", table => table.ExcludeFromMigrations());
        conversation.HasKey(x => x.ConversationId);
        conversation.Property(x => x.ContextType)
            .IsRequired()
            .HasConversion<byte>();
        conversation.Property(x => x.LastMessageAt)
            .HasColumnType("datetime2");
        conversation.Property(x => x.BuyerLastReadAt)
            .HasColumnType("datetime2");
        conversation.Property(x => x.SellerLastReadAt)
            .HasColumnType("datetime2");
        conversation.HasIndex(x => x.BuyerUserId);
        conversation.HasIndex(x => x.SellerUserId);
        conversation.HasIndex(x => x.LastMessageAt);

        var commerceMessage = builder.Entity<ConversationMessage>();
        commerceMessage.ToTable("MessagingMessages", table => table.ExcludeFromMigrations());
        commerceMessage.HasKey(x => x.MessageId);
        commerceMessage.Property(x => x.Body)
            .IsRequired()
            .HasMaxLength(2000);
        commerceMessage.Property(x => x.AttachmentUrl)
            .HasMaxLength(400);
        commerceMessage.Property(x => x.SentAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        commerceMessage.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
        commerceMessage.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        commerceMessage.HasIndex(x => new { x.ConversationId, x.SentAt })
            .IsDescending(false, true);

        var qaReview = builder.Entity<QaReview>();
        qaReview.ToTable("QaReviews", table => table.ExcludeFromMigrations());
        qaReview.HasKey(x => x.QaReviewId);
        qaReview.Property(x => x.QaEvaluationTemplateId)
            .IsRequired();
        qaReview.Property(x => x.AgentUserId)
            .IsRequired();
        qaReview.Property(x => x.ReviewerName)
            .IsRequired()
            .HasMaxLength(200);
        qaReview.Property(x => x.Notes)
            .HasMaxLength(4000);
        qaReview.Property(x => x.OverallPercent)
            .HasColumnType("decimal(5,2)");
        qaReview.Property(x => x.InlineCommentsJson)
            .HasDefaultValue("{}");
        qaReview.Property(x => x.CreatedAtUtc)
            .IsRequired();
        qaReview.Property(x => x.UpdatedAtUtc)
            .IsRequired();
        qaReview.HasOne(x => x.SupportFaq)
            .WithOne(x => x.QaReview)
            .HasForeignKey<QaReview>(x => x.SupportFaqId)
            .HasPrincipalKey<SupportFaqRecord>(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);
        qaReview.HasOne(x => x.EvaluationTemplate)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.QaEvaluationTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        qaReview.HasIndex(x => x.SupportFaqId)
            .IsUnique();
        qaReview.HasIndex(x => x.AgentUserId);
        qaReview.HasIndex(x => x.QaEvaluationTemplateId);

        var qaEvaluationTemplate = builder.Entity<QaEvaluationTemplate>();
        qaEvaluationTemplate.ToTable("QaEvaluationTemplates", table => table.ExcludeFromMigrations());
        qaEvaluationTemplate.HasKey(x => x.QaEvaluationTemplateId);
        qaEvaluationTemplate.Property(x => x.VersionNumber)
            .IsRequired();
        qaEvaluationTemplate.Property(x => x.IsActive)
            .IsRequired();
        qaEvaluationTemplate.Property(x => x.CreatedById)
            .IsRequired();
        qaEvaluationTemplate.Property(x => x.UpdatedById)
            .IsRequired();
        qaEvaluationTemplate.Property(x => x.CreatedAtUtc)
            .IsRequired();
        qaEvaluationTemplate.Property(x => x.UpdatedAtUtc)
            .IsRequired();
        qaEvaluationTemplate.Property(x => x.ActivatedAtUtc);
        qaEvaluationTemplate.HasIndex(x => x.VersionNumber)
            .IsUnique();
        qaEvaluationTemplate.HasIndex(x => x.IsActive);

        var qaEvaluationCategory = builder.Entity<QaEvaluationCategory>();
        qaEvaluationCategory.ToTable("QaEvaluationCategories", table => table.ExcludeFromMigrations());
        qaEvaluationCategory.HasKey(x => x.QaEvaluationCategoryId);
        qaEvaluationCategory.Property(x => x.CreatedById)
            .IsRequired();
        qaEvaluationCategory.Property(x => x.UpdatedById)
            .IsRequired();
        qaEvaluationCategory.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);
        qaEvaluationCategory.Property(x => x.WeightPercent)
            .HasColumnType("decimal(5,2)");
        qaEvaluationCategory.Property(x => x.DisplayOrder)
            .IsRequired();
        qaEvaluationCategory.HasOne(x => x.Template)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.QaEvaluationTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        qaEvaluationCategory.HasIndex(x => new { x.QaEvaluationTemplateId, x.DisplayOrder })
            .IsUnique();

        var qaEvaluationQuestion = builder.Entity<QaEvaluationQuestion>();
        qaEvaluationQuestion.ToTable("QaEvaluationQuestions", table => table.ExcludeFromMigrations());
        qaEvaluationQuestion.HasKey(x => x.QaEvaluationQuestionId);
        qaEvaluationQuestion.Property(x => x.CreatedById)
            .IsRequired();
        qaEvaluationQuestion.Property(x => x.UpdatedById)
            .IsRequired();
        qaEvaluationQuestion.Property(x => x.QuestionKey)
            .IsRequired()
            .HasMaxLength(80);
        qaEvaluationQuestion.Property(x => x.Prompt)
            .IsRequired()
            .HasMaxLength(400);
        qaEvaluationQuestion.Property(x => x.DisplayOrder)
            .IsRequired();
        qaEvaluationQuestion.HasOne(x => x.Category)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.QaEvaluationCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        qaEvaluationQuestion.HasIndex(x => new { x.QaEvaluationCategoryId, x.DisplayOrder })
            .IsUnique();
        qaEvaluationQuestion.HasIndex(x => new { x.QaEvaluationCategoryId, x.QuestionKey })
            .IsUnique();

        var qaReviewCategoryScore = builder.Entity<QaReviewCategoryScore>();
        qaReviewCategoryScore.ToTable("QaReviewCategoryScores", table => table.ExcludeFromMigrations());
        qaReviewCategoryScore.HasKey(x => x.QaReviewCategoryScoreId);
        qaReviewCategoryScore.Property(x => x.CategoryNameSnapshot)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue(string.Empty);
        qaReviewCategoryScore.Property(x => x.WeightPercentSnapshot)
            .HasColumnType("decimal(5,2)");
        qaReviewCategoryScore.Property(x => x.AverageScore)
            .HasColumnType("decimal(5,2)");
        qaReviewCategoryScore.Property(x => x.WeightedPoints)
            .HasColumnType("decimal(5,2)");
        qaReviewCategoryScore.Property(x => x.DisplayOrder)
            .IsRequired();
        qaReviewCategoryScore.HasOne(x => x.Review)
            .WithMany(x => x.CategoryScores)
            .HasForeignKey(x => x.QaReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        qaReviewCategoryScore.HasOne(x => x.EvaluationCategory)
            .WithMany(x => x.ReviewScores)
            .HasForeignKey(x => x.QaEvaluationCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        qaReviewCategoryScore.HasIndex(x => new { x.QaReviewId, x.DisplayOrder })
            .IsUnique();
        qaReviewCategoryScore.HasIndex(x => x.QaEvaluationCategoryId);

        var qaReviewInlineCommentDraft = builder.Entity<QaReviewInlineCommentDraft>();
        qaReviewInlineCommentDraft.ToTable("QaReviewInlineCommentDrafts", table => table.ExcludeFromMigrations());
        qaReviewInlineCommentDraft.HasKey(x => x.QaReviewInlineCommentDraftId);
        qaReviewInlineCommentDraft.Property(x => x.AgentUserId)
            .IsRequired();
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
        qaReviewInlineCommentDraft.HasOne(x => x.SupportFaq)
            .WithOne(x => x.QaReviewInlineCommentDraft)
            .HasForeignKey<QaReviewInlineCommentDraft>(x => x.SupportFaqId)
            .HasPrincipalKey<SupportFaqRecord>(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);
        qaReviewInlineCommentDraft.HasIndex(x => x.SupportFaqId)
            .IsUnique();
        qaReviewInlineCommentDraft.HasIndex(x => x.AgentUserId);

        var agentReviewFeedback = builder.Entity<AgentReviewFeedback>();
        agentReviewFeedback.ToTable("AgentReviewFeedback", table => table.ExcludeFromMigrations());
        agentReviewFeedback.HasKey(x => x.AgentReviewFeedbackId);
        agentReviewFeedback.Property(x => x.Notes)
            .IsRequired()
            .HasMaxLength(4000)
            .HasDefaultValue(string.Empty);
        agentReviewFeedback.Property(x => x.Acknowledged)
            .IsRequired()
            .HasDefaultValue(false);
        agentReviewFeedback.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        agentReviewFeedback.Property(x => x.UpdatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        agentReviewFeedback.HasIndex(x => new { x.SupportFaqId, x.AgentUserId })
            .IsUnique();

        var agentRanking = builder.Entity<AgentRanking>();
        agentRanking.ToTable("AgentRankings", "dbo", table => table.ExcludeFromMigrations());
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
        qaReviewQuestionScore.ToTable("QaReviewQuestionScores", table => table.ExcludeFromMigrations());
        qaReviewQuestionScore.HasKey(x => x.QaReviewQuestionScoreId);
        qaReviewQuestionScore.Property(x => x.QuestionKey)
            .IsRequired()
            .HasMaxLength(80);
        qaReviewQuestionScore.Property(x => x.CategoryNameSnapshot)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue(string.Empty);
        qaReviewQuestionScore.Property(x => x.QuestionTextSnapshot)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);
        qaReviewQuestionScore.Property(x => x.Score)
            .IsRequired();
        qaReviewQuestionScore.HasOne(x => x.Review)
            .WithMany(x => x.QuestionScores)
            .HasForeignKey(x => x.QaReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        qaReviewQuestionScore.HasOne(x => x.EvaluationQuestion)
            .WithMany(x => x.ReviewScores)
            .HasForeignKey(x => x.QaEvaluationQuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        qaReviewQuestionScore.HasIndex(x => new { x.QaReviewId, x.QuestionKey })
            .IsUnique();
    }
}
