using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.HelpCenter;

public sealed class SupportContactChannel
{
    public int SupportContactChannelId { get; set; }

    [MaxLength(40)]
    public string ChannelType { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(320)]
    public string DisplayText { get; set; } = string.Empty;

    [MaxLength(400)]
    public string ActionHref { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
