namespace NextHorizon.Data;

public sealed class SupportAgentRecord
{
    public int ChatId { get; set; }

    public int? ConversationId { get; set; }

    public string? AgentName { get; set; }

    public string? ClientName { get; set; }

    public string? Category { get; set; }

    public string? PreviewQuestion { get; set; }

    public string? ChatStatus { get; set; }

    public string? AgentStatus { get; set; }

    public int? AgentId { get; set; }

    public int UserId { get; set; }

    public int? ChatSlot { get; set; }

    public string? Notes { get; set; }

    public DateTime? NotesLastUpdatedAt { get; set; }

    public DateTime? ACWStartTime { get; set; }

    public DateTime? ACWEndTime { get; set; }
}
