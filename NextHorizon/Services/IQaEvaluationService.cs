using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public interface IQaEvaluationService
{
    Task<QaEvaluationsPageData> GetPageDataAsync(CancellationToken cancellationToken);

    Task<QaEvaluationTemplateViewModel?> GetActiveTemplateAsync(CancellationToken cancellationToken);

    Task<QaEvaluationTemplateViewModel?> GetTemplateAsync(int templateId, CancellationToken cancellationToken);

    Task<QaEvaluationTemplateMutationResponse> SaveTemplateAsync(
        QaEvaluationTemplateUpsertRequest request,
        int updatedById,
        string updatedByName,
        CancellationToken cancellationToken);

    Task<QaEvaluationTemplateMutationResponse> TrackReuseAsync(
        int templateId,
        string updatedByName,
        CancellationToken cancellationToken);
}
