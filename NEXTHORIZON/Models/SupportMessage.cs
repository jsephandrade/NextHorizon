namespace NextHorizon.Models
{
    public class SupportMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}