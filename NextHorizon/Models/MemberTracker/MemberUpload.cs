namespace MemberTracker.Models;

public class MemberUpload
{
    public int UploadId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ActivityName { get; set; } = string.Empty;

    public DateTime ActivityDate { get; set; }

    public string ProofUrl { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public int MovingTimeSec { get; set; }

    public int? Steps { get; set; }

    public int? AvgPaceSecPerKm { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
