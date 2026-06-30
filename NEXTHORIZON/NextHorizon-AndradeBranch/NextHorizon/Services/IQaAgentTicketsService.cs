using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaAgentTicketsService
{
    Task<QaAgentTicketsResponse?> GetAgentTicketsAsync(
        int agentUserId,
        int awaitingPage,
        int ratedPage,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        string? score,
        CancellationToken cancellationToken);
}
