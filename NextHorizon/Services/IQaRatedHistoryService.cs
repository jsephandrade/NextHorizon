using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaRatedHistoryService
{
    Task<QaRatedHistoryResponse> GetRatedHistoryAsync(
        int page,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        string? score,
        CancellationToken cancellationToken);
}
