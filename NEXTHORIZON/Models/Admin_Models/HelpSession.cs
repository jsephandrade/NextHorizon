using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models   // ← must match what AppDbContext uses
{
    [Table("SupportFAQs", Schema = "dbo")]
    public class HelpSession
    {
        [Key]
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Question { get; set; }
        public int? DurationMinutes { get; set; }
        public string? UserType { get; set; }
        public int? AgentId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}