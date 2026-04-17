using NextHorizon.Models.Agent;

namespace NextHorizon.Services;

public interface IAgentRankingService
{
    Task RecomputeMonthAsync(AgentRankingMetricType metricType, DateTime periodStartUtc, CancellationToken cancellationToken);
    Task RecomputeAllQaMonthsAsync(CancellationToken cancellationToken);
}
