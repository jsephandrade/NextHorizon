using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaDashboardService
{
    Task<QaDashboardResponse> GetDashboardAsync(DateOnly selectedDate, CancellationToken cancellationToken);
}
