using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.HelpCenter;

public enum SupportTicketStatus : byte
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
}

public sealed class SupportTicket
{
    public int SupportTicketId { get; set; }

    [MaxLength(40)]
    public string ReferenceCode { get; set; } = string.Empty;

    public int UserId { get; set; }

    public int? ConsumerId { get; set; }

    [MaxLength(120)]
    public string? FaqCategory { get; set; }

    [MaxLength(160)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

