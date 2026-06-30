namespace NextHorizon.Models.AgentDashboard;

public sealed class AgentReviewFeedback
{
    public int AgentReviewFeedbackId { get; set; }

    public int SupportFaqId { get; set; }

    public int AgentUserId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool Acknowledged { get; set; }

    public DateTime? AcknowledgedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
