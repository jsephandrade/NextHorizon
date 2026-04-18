using NextHorizon.Models.HelpCenter;

namespace NextHorizon.Models.QA;

public sealed class QaReview
{
    public int QaReviewId { get; set; }

    public int SupportFaqId { get; set; }

    public int AgentUserId { get; set; }

    public int ReviewerStaffId { get; set; }

    public string ReviewerName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public decimal AccuracyAverage { get; set; }

    public decimal ToneAverage { get; set; }

    public decimal ResolutionAverage { get; set; }

    public decimal OverallPercent { get; set; }

    public bool SubmittedToAgent { get; set; }

    public DateTime? SubmittedToAgentAtUtc { get; set; }

    public string InlineCommentsJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public SupportFaqRecord? SupportFaq { get; set; }

    public ICollection<QaReviewQuestionScore> QuestionScores { get; set; } = new List<QaReviewQuestionScore>();
}

public sealed class QaReviewQuestionScore
{
    public int QaReviewQuestionScoreId { get; set; }

    public int QaReviewId { get; set; }

    public string QuestionKey { get; set; } = string.Empty;

    public int Score { get; set; }

    public QaReview? Review { get; set; }
}

public sealed class QaReviewInlineCommentDraft
{
    public int QaReviewInlineCommentDraftId { get; set; }

    public int SupportFaqId { get; set; }

    public int AgentUserId { get; set; }

    public int UpdatedByStaffId { get; set; }

    public string UpdatedByName { get; set; } = string.Empty;

    public string InlineCommentsJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public SupportFaqRecord? SupportFaq { get; set; }
}
