using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.HelpCenter;

public sealed class FaqRecord
{
    public int FaqId { get; set; }

    [MaxLength(500)]
    public string Question { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Answer { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime DateAdded { get; set; }

    public DateTime LastUpdated { get; set; }

    [MaxLength(40)]
    public string UserType { get; set; } = string.Empty;
}
