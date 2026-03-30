using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.HelpCenter;

public enum LiveAgentSessionStatus : byte
{
    Waiting = 1,
    Active = 2,
    Resolved = 3,
}

public enum LiveAgentSessionEndedReason : byte
{
    None = 0,
    Resolved = 1,
    Inactive = 2,
}

public sealed class LiveAgentSession
{
    public int LiveAgentSessionId { get; set; }

    public int SupportFaqId { get; set; }

    public int UserId { get; set; }

    public int? ConsumerId { get; set; }

    [MaxLength(80)]
    public string CategorySlug { get; set; } = string.Empty;

    [MaxLength(120)]
    public string CategoryTitle { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string FirstQuestion { get; set; } = string.Empty;

    public LiveAgentSessionStatus Status { get; set; } = LiveAgentSessionStatus.Waiting;

    public LiveAgentSessionEndedReason EndedReason { get; set; } = LiveAgentSessionEndedReason.None;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
