using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaRatingQueueService
{
    Task<QaRatingQueueResponse> GetQueueAsync(
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        int? ticketId,
        CancellationToken cancellationToken);
}
