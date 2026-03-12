using Microsoft.AspNetCore.Http;

namespace MemberTracker.Models;

public class UpdateMemberUploadRequest
{
    public string Title { get; init; } = string.Empty;

    public string ActivityName { get; init; } = string.Empty;

    public DateTime ActivityDate { get; init; }

    public decimal? DistanceKm { get; init; }

    public decimal? DistanceMi { get; init; }

    public int MovingTimeSec { get; init; }

    public int? Steps { get; init; }

    public IFormFile? Proof { get; init; }
}
