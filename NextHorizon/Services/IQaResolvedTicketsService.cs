using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaResolvedTicketsService
{
    Task<QaResolvedTicketsResponse> GetResolvedTicketsAsync(
        int page,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken);
}
