using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaDashboardService
{
    Task<QaDashboardResponse> GetDashboardAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
