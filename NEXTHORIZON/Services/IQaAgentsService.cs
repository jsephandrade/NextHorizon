using NextHorizon.Models;

namespace NextHorizon.Services;

public interface IQaAgentsService
{
    Task<IReadOnlyList<AgentSummaryViewModel>> GetAgentsAsync(CancellationToken cancellationToken);
}
