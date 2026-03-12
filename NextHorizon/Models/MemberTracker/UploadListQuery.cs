namespace MemberTracker.Models;

public class UploadListQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Sort { get; init; }

    // Backward-compatible aliases for existing clients.
    public int? PageNumber { get; init; }

    public string? SortBy { get; init; }
}
