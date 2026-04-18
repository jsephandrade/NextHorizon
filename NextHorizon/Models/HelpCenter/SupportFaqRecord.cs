using NextHorizon.Models.QA;

namespace NextHorizon.Models.HelpCenter;

public sealed class SupportFaqRecord
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public string UserType { get; set; } = string.Empty;

    public int? AgentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EndTime { get; set; }

    public DateTime? StartTime { get; set; }

    public QaReview? QaReview { get; set; }

    public QaReviewInlineCommentDraft? QaReviewInlineCommentDraft { get; set; }
}
