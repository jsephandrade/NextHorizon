namespace NextHorizon.Models.AgentDashboard;

public enum AgentRankingMetricType : byte
{
    QaScore = 1,
    AverageHandlingTime = 2
}

public sealed class AgentRanking
{
    public int AgentRankingId { get; set; }

    public int AgentUserId { get; set; }

    public DateTime PeriodStartUtc { get; set; }

    public AgentRankingMetricType MetricType { get; set; } = AgentRankingMetricType.QaScore;

    public decimal MetricValue { get; set; }

    public int ReviewCount { get; set; }

    public int RankPosition { get; set; }

    public int RankedAgentCount { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
