using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.HelpCenter;

public sealed class HelpFaq
{
    public int HelpFaqId { get; set; }

    public int HelpCategoryId { get; set; }

    [MaxLength(200)]
    public string Question { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Answer { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? SearchKeywords { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeaturedOnHome { get; set; }

    public HelpCategory? Category { get; set; }
}
