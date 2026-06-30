using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaReviewService
{
    Task<QaRatingPageData?> GetRatingPageAsync(
        int supportFaqId,
        string reviewerDisplayName,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken);

    Task<QaReviewMutationResponse> UpsertReviewAsync(
        int supportFaqId,
        int reviewerStaffId,
        string reviewerName,
        QaReviewUpsertRequest request,
        CancellationToken cancellationToken);

    Task<QaReviewMutationResponse> UpsertInlineCommentsAsync(
        int supportFaqId,
        int reviewerStaffId,
        string reviewerName,
        QaInlineCommentsUpsertRequest request,
        CancellationToken cancellationToken);

    Task<QaReviewMutationResponse> SubmitToAgentAsync(
        int supportFaqId,
        int reviewerStaffId,
        string reviewerName,
        CancellationToken cancellationToken);
}
