using NextHorizon.Models.HelpCenter;

namespace NextHorizon.Data;

internal static class HelpCenterSeed
{
    public static readonly SupportContactChannel[] ContactChannels =
    {
        new()
        {
            SupportContactChannelId = 1,
            ChannelType = "email",
            Label = "Email Support",
            Value = "support@nexthorizon.com",
            DisplayText = "support@nexthorizon.com",
            ActionHref = "mailto:support@nexthorizon.com",
            DisplayOrder = 1,
            IsActive = true,
        },
        new()
        {
            SupportContactChannelId = 2,
            ChannelType = "phone",
            Label = "Phone Support",
            Value = "+18001234567",
            DisplayText = "+1 (800) 123-4567",
            ActionHref = "tel:+18001234567",
            DisplayOrder = 2,
            IsActive = true,
        },
        new()
        {
            SupportContactChannelId = 3,
            ChannelType = "chat",
            Label = "Live Chat",
            Value = "/consumer/messenger/",
            DisplayText = "Open live support chat",
            ActionHref = "/consumer/messenger/",
            DisplayOrder = 3,
            IsActive = true,
        },
        new()
        {
            SupportContactChannelId = 4,
            ChannelType = "hours",
            Label = "Support Hours",
            Value = "Mon-Fri, 9AM-6PM (local time)",
            DisplayText = "Mon-Fri, 9AM-6PM (local time)",
            ActionHref = string.Empty,
            DisplayOrder = 4,
            IsActive = true,
        },
    };
}
