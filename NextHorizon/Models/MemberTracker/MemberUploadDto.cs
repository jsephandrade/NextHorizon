namespace MemberTracker.Models;

public class MemberUploadDto
{
    public int UploadId { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string ActivityName { get; init; } = string.Empty;

    public DateTime ActivityDate { get; init; }

    public string ProofUrl { get; init; } = string.Empty;

    public decimal DistanceKm { get; init; }

    public decimal DistanceMi { get; init; }

    public int MovingTimeSec { get; init; }

    public int? Steps { get; init; }

    public int? AvgPaceSecPerKm { get; init; }

    public int? AvgPaceSecPerMi { get; init; }

    public bool IsPaceSuspicious { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
