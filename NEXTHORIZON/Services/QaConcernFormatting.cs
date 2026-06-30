namespace NextHorizon.Services;

internal static class QaConcernFormatting
{
    public static string NormalizeConcernFrom(string? userType)
    {
        if (string.IsNullOrWhiteSpace(userType))
        {
            return "Unknown";
        }

        var normalized = userType.Trim();
        if (normalized.Length == 0)
        {
            return "Unknown";
        }

        if (string.Equals(normalized, "seller", StringComparison.OrdinalIgnoreCase))
        {
            return "Seller";
        }

        if (string.Equals(normalized, "consumer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "customer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "buyer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "user", StringComparison.OrdinalIgnoreCase))
        {
            return "Customer";
        }

        return normalized;
    }

    public static string NormalizeReviewerName(string? reviewerName)
    {
        return string.IsNullOrWhiteSpace(reviewerName) ? "QA Reviewer" : reviewerName.Trim();
    }
}
