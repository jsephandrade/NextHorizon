using NextHorizon.Models.HelpCenter;

namespace NextHorizon.Models.QA;

public sealed class QaReview
{
    public int QaReviewId { get; set; }

    public int SupportFaqId { get; set; }

    public int QaEvaluationTemplateId { get; set; }

    public int AgentUserId { get; set; }

    public int ReviewerStaffId { get; set; }

    public string ReviewerName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public decimal OverallPercent { get; set; }

    public bool SubmittedToAgent { get; set; }

    public DateTime? SubmittedToAgentAtUtc { get; set; }

    public string InlineCommentsJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public SupportFaqRecord? SupportFaq { get; set; }

    public QaEvaluationTemplate? EvaluationTemplate { get; set; }

    public ICollection<QaReviewCategoryScore> CategoryScores { get; set; } = new List<QaReviewCategoryScore>();

    public ICollection<QaReviewQuestionScore> QuestionScores { get; set; } = new List<QaReviewQuestionScore>();
}

public sealed class QaReviewCategoryScore
{
    public int QaReviewCategoryScoreId { get; set; }

    public int QaReviewId { get; set; }

    public int? QaEvaluationCategoryId { get; set; }

    public string CategoryNameSnapshot { get; set; } = string.Empty;

    public decimal WeightPercentSnapshot { get; set; }

    public decimal AverageScore { get; set; }

    public decimal WeightedPoints { get; set; }

    public int DisplayOrder { get; set; }

    public QaReview? Review { get; set; }

    public QaEvaluationCategory? EvaluationCategory { get; set; }
}

public sealed class QaReviewQuestionScore
{
    public int QaReviewQuestionScoreId { get; set; }

    public int QaReviewId { get; set; }

    public int? QaEvaluationQuestionId { get; set; }

    public string QuestionKey { get; set; } = string.Empty;

    public string CategoryNameSnapshot { get; set; } = string.Empty;

    public string QuestionTextSnapshot { get; set; } = string.Empty;

    public int Score { get; set; }

    public QaReview? Review { get; set; }

    public QaEvaluationQuestion? EvaluationQuestion { get; set; }
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
