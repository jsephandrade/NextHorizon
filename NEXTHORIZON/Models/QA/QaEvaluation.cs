namespace NextHorizon.Models.QA;

public sealed class QaEvaluationTemplate
{
    public int QaEvaluationTemplateId { get; set; }

    public int VersionNumber { get; set; }

    public bool IsActive { get; set; }

    public int CreatedById { get; set; }

    public int UpdatedById { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }

    public ICollection<QaEvaluationCategory> Categories { get; set; } = new List<QaEvaluationCategory>();

    public ICollection<QaReview> Reviews { get; set; } = new List<QaReview>();
}

public sealed class QaEvaluationCategory
{
    public int QaEvaluationCategoryId { get; set; }

    public int QaEvaluationTemplateId { get; set; }

    public int CreatedById { get; set; }

    public int UpdatedById { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal WeightPercent { get; set; }

    public int DisplayOrder { get; set; }

    public QaEvaluationTemplate? Template { get; set; }

    public ICollection<QaEvaluationQuestion> Questions { get; set; } = new List<QaEvaluationQuestion>();

    public ICollection<QaReviewCategoryScore> ReviewScores { get; set; } = new List<QaReviewCategoryScore>();
}

public sealed class QaEvaluationQuestion
{
    public int QaEvaluationQuestionId { get; set; }

    public int QaEvaluationCategoryId { get; set; }

    public int CreatedById { get; set; }

    public int UpdatedById { get; set; }

    public string QuestionKey { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public QaEvaluationCategory? Category { get; set; }

    public ICollection<QaReviewQuestionScore> ReviewScores { get; set; } = new List<QaReviewQuestionScore>();
}
