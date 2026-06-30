using NextHorizon.Models.AgentDashboard;

namespace NextHorizon.Services;

public interface IAgentDashboardService
{
    Task<AgentDashboardResponse> GetDashboardAsync(int agentUserId, CancellationToken cancellationToken);
    Task<AgentDashboardTicketDetail?> GetTicketDetailAsync(int agentUserId, int supportFaqId, CancellationToken cancellationToken);
    Task<AgentDashboardMutationResponse> SaveNotesAsync(int agentUserId, int supportFaqId, string? notes, CancellationToken cancellationToken);
    Task<AgentDashboardMutationResponse> AcknowledgeAsync(int agentUserId, int supportFaqId, CancellationToken cancellationToken);
}
