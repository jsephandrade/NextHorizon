using NextHorizon.Models.HelpCenter;

namespace NextHorizon.Data;

internal static class HelpCenterSeed
{
    public static readonly HelpCategory[] Categories =
    {
        new()
        {
            HelpCategoryId = 1,
            Slug = "order",
            Title = "Order Help",
            Description = "Order updates, cancellations, changes, and confirmation questions.",
            IconKey = "fa-solid fa-box",
            DisplayOrder = 1,
            IsActive = true,
        },
        new()
        {
            HelpCategoryId = 2,
            Slug = "tracking",
            Title = "Order Tracking",
            Description = "Delivery timing, shipment visibility, and courier status updates.",
            IconKey = "fa-solid fa-truck",
            DisplayOrder = 2,
            IsActive = true,
        },
        new()
        {
            HelpCategoryId = 3,
            Slug = "billing",
            Title = "Billing & Payments",
            Description = "Payment methods, declines, taxes, and refund processing details.",
            IconKey = "fa-solid fa-credit-card",
            DisplayOrder = 3,
            IsActive = true,
        },
        new()
        {
            HelpCategoryId = 4,
            Slug = "account",
            Title = "Account Help",
            Description = "Profile changes, password recovery, login issues, and security settings.",
            IconKey = "fa-solid fa-user-gear",
            DisplayOrder = 4,
            IsActive = true,
        },
        new()
        {
            HelpCategoryId = 5,
            Slug = "returns",
            Title = "Returns & Refunds",
            Description = "Return windows, damaged items, shipping responsibility, and refunds.",
            IconKey = "fa-solid fa-rotate-left",
            DisplayOrder = 5,
            IsActive = true,
        },
        new()
        {
            HelpCategoryId = 6,
            Slug = "technical",
            Title = "Technical Support",
            Description = "Site issues, app crashes, login trouble, and browser or device problems.",
            IconKey = "fa-solid fa-screwdriver-wrench",
            DisplayOrder = 6,
            IsActive = true,
        },
        new()
        {
            HelpCategoryId = 7,
            Slug = "contact",
            Title = "Contact Support",
            Description = "Support channels, response times, callbacks, and partnership inquiries.",
            IconKey = "fa-solid fa-life-ring",
            DisplayOrder = 7,
            IsActive = true,
        },
    };

    public static readonly HelpFaq[] Faqs =
    {
        new() { HelpFaqId = 1, HelpCategoryId = 1, Question = "Can I cancel my order after placing it?", Answer = "Yes, if your order has not shipped yet. Go to My Purchases, select the order, and click Cancel Order. Once shipped, you will need to request a return after delivery.", SearchKeywords = "cancel order checkout shipping", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = true },
        new() { HelpFaqId = 2, HelpCategoryId = 1, Question = "What does \"Processing\" mean?", Answer = "Your order is being prepared for shipment. This typically takes 1 to 2 business days before it is handed to the courier.", SearchKeywords = "processing status", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 3, HelpCategoryId = 1, Question = "Can I modify my order after checkout?", Answer = "Order modifications such as address or item changes are only possible before processing begins. Contact support immediately if you need changes.", SearchKeywords = "modify address items", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 4, HelpCategoryId = 1, Question = "How do I know if my order was successful?", Answer = "You will receive an order confirmation email immediately after checkout. You can also check your order status in My Purchases.", SearchKeywords = "confirmation successful email", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 5, HelpCategoryId = 1, Question = "Can I add items to an existing order?", Answer = "No, items cannot be added to an existing order. Please place a new order for additional items.", SearchKeywords = "add items existing order", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },

        new() { HelpFaqId = 6, HelpCategoryId = 2, Question = "Where is my order?", Answer = "Track it in My Purchases and then Track Order for real-time updates, carrier info, and estimated delivery.", SearchKeywords = "track order delivery", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = true },
        new() { HelpFaqId = 7, HelpCategoryId = 2, Question = "How long will delivery take?", Answer = "Standard delivery takes 3 to 7 business days. Express delivery takes 1 to 3 business days. Times vary by location and carrier conditions.", SearchKeywords = "delivery time express standard", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 8, HelpCategoryId = 2, Question = "Why is my order delayed?", Answer = "Delays may happen because of weather, logistics issues, customs for international shipments, or high order volume. Check tracking for updates.", SearchKeywords = "delayed shipment logistics customs", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 9, HelpCategoryId = 2, Question = "Can I receive my order earlier?", Answer = "Delivery speed depends on the courier service selected at checkout. Upgrade to Express at checkout for faster delivery.", SearchKeywords = "faster delivery express", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 10, HelpCategoryId = 2, Question = "What if I am not home during delivery?", Answer = "The courier will leave a notice with redelivery instructions. You can also arrange pickup at a nearby collection point.", SearchKeywords = "missed delivery redelivery pickup", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 11, HelpCategoryId = 2, Question = "Do you ship internationally?", Answer = "Yes. We ship to more than 50 countries. International shipping times and fees vary, and customs duties may apply.", SearchKeywords = "international shipping countries customs", DisplayOrder = 6, IsActive = true, IsFeaturedOnHome = false },

        new() { HelpFaqId = 12, HelpCategoryId = 3, Question = "What payment methods are accepted?", Answer = "Visa, Mastercard, American Express, PayPal, Apple Pay, Google Pay, and Cash on Delivery in select regions are accepted.", SearchKeywords = "payment methods cards paypal cod", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 13, HelpCategoryId = 3, Question = "Why was my payment declined?", Answer = "Common reasons include insufficient funds, incorrect card details, a bank security hold, or billing address mismatch. Try another method or contact your bank.", SearchKeywords = "declined payment billing mismatch", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = true },
        new() { HelpFaqId = 14, HelpCategoryId = 3, Question = "Can I change payment method after ordering?", Answer = "Payment methods cannot be changed after checkout. Cancel the order if it has not shipped and place a new one with the correct payment method.", SearchKeywords = "change payment after order", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 15, HelpCategoryId = 3, Question = "Is my payment information secure?", Answer = "Yes. We use PCI-DSS compliant encryption and never store full card details. Transactions run through secure payment gateways.", SearchKeywords = "secure payment pci dss encryption", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 16, HelpCategoryId = 3, Question = "Can I save multiple payment methods?", Answer = "Yes. Go to Account Settings and then Payment Methods to save multiple cards for faster checkout.", SearchKeywords = "save cards payment methods", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 17, HelpCategoryId = 3, Question = "Do you charge sales tax?", Answer = "Sales tax is calculated from your shipping address and local regulations. Tax details appear at checkout before payment.", SearchKeywords = "sales tax checkout", DisplayOrder = 6, IsActive = true, IsFeaturedOnHome = false },

        new() { HelpFaqId = 18, HelpCategoryId = 4, Question = "I forgot my password", Answer = "Click Forgot Password on the login page. Enter your email to receive a secure reset link that stays valid for 1 hour.", SearchKeywords = "forgot password reset link", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 19, HelpCategoryId = 4, Question = "How do I change my email?", Answer = "Go to Account Settings, then Profile, then Edit Email. Verify your new email to complete the change.", SearchKeywords = "change email profile", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 20, HelpCategoryId = 4, Question = "Why is my account locked?", Answer = "Accounts lock after too many failed login attempts for security reasons. Wait 30 minutes or reset your password to unlock sooner.", SearchKeywords = "account locked failed login", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 21, HelpCategoryId = 4, Question = "How do I delete my account?", Answer = "Go to Account Settings, then Privacy, then Delete Account. This action is permanent and removes all order history.", SearchKeywords = "delete account privacy", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 22, HelpCategoryId = 4, Question = "Can I have multiple accounts?", Answer = "Each user should maintain one account. Multiple accounts may be suspended for security reasons.", SearchKeywords = "multiple accounts security", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 23, HelpCategoryId = 4, Question = "How do I enable two-factor authentication?", Answer = "Go to Account Settings, then Security, then Two-Factor Authentication. Follow the setup instructions with your authenticator app.", SearchKeywords = "2fa two-factor authentication", DisplayOrder = 6, IsActive = true, IsFeaturedOnHome = false },

        new() { HelpFaqId = 24, HelpCategoryId = 5, Question = "How do I request a refund?", Answer = "Go to your order details and click Return / Refund. Select a reason, upload photos if needed, and submit. Refunds process within 5 to 10 business days.", SearchKeywords = "refund return request", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 25, HelpCategoryId = 5, Question = "Can I return used items?", Answer = "Only unused items with original packaging, tags, and proof of purchase are accepted. Hygiene-sensitive items cannot be returned.", SearchKeywords = "used items return policy", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 26, HelpCategoryId = 5, Question = "Who pays for return shipping?", Answer = "If the item is defective or we sent the wrong item, we cover return shipping. For change-of-mind returns, the customer pays.", SearchKeywords = "return shipping defective wrong item", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 27, HelpCategoryId = 5, Question = "What if my item arrives damaged?", Answer = "Contact us within 48 hours with photos of the damage. We will arrange a free replacement or full refund.", SearchKeywords = "damaged item replacement", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 28, HelpCategoryId = 5, Question = "How long do I have to return an item?", Answer = "You have 30 days from delivery to initiate a return. After 30 days, returns are only accepted for defective items.", SearchKeywords = "return window 30 days", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 29, HelpCategoryId = 5, Question = "When will I receive my refund?", Answer = "Refunds are processed within 5 to 10 business days after we receive and inspect your return, and the amount goes back to your original payment method.", SearchKeywords = "refund timing original payment method", DisplayOrder = 6, IsActive = true, IsFeaturedOnHome = false },

        new() { HelpFaqId = 30, HelpCategoryId = 6, Question = "The app keeps crashing", Answer = "Force close and reopen the app, clear the app cache, update to the latest version, or reinstall it. Contact support if the issue continues.", SearchKeywords = "app crashing cache reinstall", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 31, HelpCategoryId = 6, Question = "Website not loading properly", Answer = "Clear browser cache and cookies, disable browser extensions, try incognito mode, or switch browsers such as Chrome, Firefox, or Safari.", SearchKeywords = "website loading browser cache cookies", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 32, HelpCategoryId = 6, Question = "I cannot log in to my account", Answer = "Check that your email and password are correct, make sure Caps Lock is off, and confirm your internet connection. Use Forgot Password if needed.", SearchKeywords = "cannot log in password", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 33, HelpCategoryId = 6, Question = "Payment page will not load", Answer = "This may be caused by browser security settings. Allow pop-ups for our site, disable ad blockers temporarily, or try another browser or device.", SearchKeywords = "payment page pop-ups ad blocker", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 34, HelpCategoryId = 6, Question = "Images are not loading on the app", Answer = "Check your internet connection, clear the app cache, or switch between Wi-Fi and mobile data.", SearchKeywords = "images not loading app", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 35, HelpCategoryId = 6, Question = "How do I update the app?", Answer = "Open your device app store, search for our app, and tap Update if a newer version is available.", SearchKeywords = "update app store", DisplayOrder = 6, IsActive = true, IsFeaturedOnHome = false },

        new() { HelpFaqId = 36, HelpCategoryId = 7, Question = "How do I contact customer support?", Answer = "Email support@nexthorizon.com, use live chat from the site, or call +1 (800) 123-4567 during business hours.", SearchKeywords = "contact support email live chat phone", DisplayOrder = 1, IsActive = true, IsFeaturedOnHome = true },
        new() { HelpFaqId = 37, HelpCategoryId = 7, Question = "What is your response time?", Answer = "Live chat is immediate when available, email responses arrive within 24 business hours, and urgent issues are prioritized.", SearchKeywords = "response time urgent issues", DisplayOrder = 2, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 38, HelpCategoryId = 7, Question = "How do I file a complaint or suggestion?", Answer = "Email support@nexthorizon.com with the subject Feedback and include your order number if applicable plus a detailed description.", SearchKeywords = "complaint suggestion feedback", DisplayOrder = 3, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 39, HelpCategoryId = 7, Question = "Do you support media or partnership inquiries?", Answer = "Yes. Email support@nexthorizon.com with the subject Media Inquiry or Partnership, and our team will respond within 48 hours.", SearchKeywords = "media partnership inquiry", DisplayOrder = 4, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 40, HelpCategoryId = 7, Question = "What are your support hours?", Answer = "Live chat is available 24/7. Email is monitored daily with responses within 24 hours. Phone support runs Monday through Friday from 9AM to 6PM local time.", SearchKeywords = "support hours phone email", DisplayOrder = 5, IsActive = true, IsFeaturedOnHome = false },
        new() { HelpFaqId = 41, HelpCategoryId = 7, Question = "Can I request a callback?", Answer = "Yes. Send us your preferred time and phone number, and we will schedule a callback during business hours.", SearchKeywords = "callback phone number", DisplayOrder = 6, IsActive = true, IsFeaturedOnHome = false },
    };

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
