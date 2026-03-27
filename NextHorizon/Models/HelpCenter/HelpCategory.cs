using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.HelpCenter;

public sealed class HelpCategory
{
    public int HelpCategoryId { get; set; }

    [MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    public string IconKey { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<HelpFaq> Faqs { get; set; } = new List<HelpFaq>();

    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}
