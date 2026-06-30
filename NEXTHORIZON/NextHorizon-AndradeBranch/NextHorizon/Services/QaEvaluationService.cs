using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaEvaluationService : IQaEvaluationService
{
    private const string QaEvaluationNotificationCategory = "QaEvaluation";
    private const string QaAnalystRole = "QA Analyst";
    private const string SupportAgentRole = "Support Agent";

    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public QaEvaluationService(ApplicationDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<QaEvaluationsPageData> GetPageDataAsync(CancellationToken cancellationToken)
    {
        var templates = await _dbContext.QaEvaluationTemplates
            .AsNoTracking()
            .Include(item => item.Categories.OrderBy(category => category.DisplayOrder))
                .ThenInclude(item => item.Questions.OrderBy(question => question.DisplayOrder))
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);

        var activeTemplate = templates.FirstOrDefault(item => item.IsActive);
        var history = templates
            .Select(item => new QaEvaluationTemplateHistoryItem(
                item.QaEvaluationTemplateId,
                item.VersionNumber,
                item.IsActive,
                item.ActivatedAtUtc?.ToString("MMM dd, yyyy hh:mm tt") ?? "Not activated",
                item.Categories.Count,
                item.Categories.Sum(category => category.Questions.Count)))
            .ToList();

        return new QaEvaluationsPageData
        {
            ActiveTemplate = activeTemplate is null ? null : MapTemplate(activeTemplate, null),
            History = history
        };
    }

    public async Task<QaEvaluationTemplateViewModel?> GetActiveTemplateAsync(CancellationToken cancellationToken)
    {
        var template = await _dbContext.QaEvaluationTemplates
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Include(item => item.Categories.OrderBy(category => category.DisplayOrder))
                .ThenInclude(item => item.Questions.OrderBy(question => question.DisplayOrder))
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return template is null ? null : MapTemplate(template, null);
    }

    public async Task<QaEvaluationTemplateViewModel?> GetTemplateAsync(int templateId, CancellationToken cancellationToken)
    {
        var template = await _dbContext.QaEvaluationTemplates
            .AsNoTracking()
            .Where(item => item.QaEvaluationTemplateId == templateId)
            .Include(item => item.Categories.OrderBy(category => category.DisplayOrder))
                .ThenInclude(item => item.Questions.OrderBy(question => question.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);

        return template is null ? null : MapTemplate(template, null);
    }

    public async Task<QaEvaluationTemplateMutationResponse> SaveTemplateAsync(
        QaEvaluationTemplateUpsertRequest request,
        int updatedById,
        string updatedByName,
        CancellationToken cancellationToken)
    {
        var normalizedCategories = NormalizeCategories(request);
        var validationMessage = Validate(normalizedCategories);
        if (validationMessage is not null)
        {
            return new QaEvaluationTemplateMutationResponse(false, validationMessage);
        }

        var nowUtc = DateTime.UtcNow;
        var nextVersionNumber = await _dbContext.QaEvaluationTemplates
            .Select(item => (int?)item.VersionNumber)
            .MaxAsync(cancellationToken) + 1 ?? 1;

        var activeTemplates = await _dbContext.QaEvaluationTemplates
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var activeTemplate in activeTemplates)
        {
            activeTemplate.IsActive = false;
            activeTemplate.UpdatedById = updatedById;
            activeTemplate.UpdatedAtUtc = nowUtc;
        }

        var template = new QaEvaluationTemplate
        {
            VersionNumber = nextVersionNumber,
            IsActive = true,
            CreatedById = updatedById,
            UpdatedById = updatedById,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            ActivatedAtUtc = nowUtc
        };

        for (var categoryIndex = 0; categoryIndex < normalizedCategories.Count; categoryIndex++)
        {
            var categoryInput = normalizedCategories[categoryIndex];
            var category = new QaEvaluationCategory
            {
                CreatedById = updatedById,
                UpdatedById = updatedById,
                Name = categoryInput.Name,
                WeightPercent = categoryInput.WeightPercent,
                DisplayOrder = categoryIndex + 1
            };

            for (var questionIndex = 0; questionIndex < categoryInput.Questions.Count; questionIndex++)
            {
                var questionInput = categoryInput.Questions[questionIndex];
                category.Questions.Add(new QaEvaluationQuestion
                {
                    CreatedById = updatedById,
                    UpdatedById = updatedById,
                    QuestionKey = BuildQuestionKey(categoryInput.Name, categoryIndex + 1, questionIndex + 1),
                    Prompt = questionInput,
                    DisplayOrder = questionIndex + 1
                });
            }

            template.Categories.Add(category);
        }

        _dbContext.QaEvaluationTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyEvaluationChangeRecipientsAsync(
            BuildPublishedNotificationMessage(updatedByName, template.VersionNumber),
            template.QaEvaluationTemplateId,
            cancellationToken);

        var viewModel = MapTemplate(template, null);
        return new QaEvaluationTemplateMutationResponse(true, "QA evaluation template saved.", viewModel);
    }

    public async Task<QaEvaluationTemplateMutationResponse> TrackReuseAsync(
        int templateId,
        string updatedByName,
        CancellationToken cancellationToken)
    {
        var template = await _dbContext.QaEvaluationTemplates
            .AsNoTracking()
            .Where(item => item.QaEvaluationTemplateId == templateId)
            .Select(item => new
            {
                item.VersionNumber
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return new QaEvaluationTemplateMutationResponse(false, "QA evaluation template not found.");
        }

        await NotifyEvaluationChangeRecipientsAsync(
            BuildReuseNotificationMessage(updatedByName, template.VersionNumber),
            templateId,
            cancellationToken);

        return new QaEvaluationTemplateMutationResponse(true, "QA evaluation reuse notification saved.");
    }

    private static List<NormalizedCategoryInput> NormalizeCategories(QaEvaluationTemplateUpsertRequest? request)
    {
        if (request?.Categories is null)
        {
            return [];
        }

        return request.Categories
            .Select(category => new NormalizedCategoryInput(
                (category.Name ?? string.Empty).Trim(),
                Math.Round(category.WeightPercent, 2, MidpointRounding.AwayFromZero),
                (category.Questions ?? [])
                    .Select(question => (question.Prompt ?? string.Empty).Trim())
                    .Where(prompt => prompt.Length > 0)
                    .ToList()))
            .Where(category => category.Name.Length > 0 || category.Questions.Count > 0 || category.WeightPercent > 0)
            .ToList();
    }

    private static string? Validate(IReadOnlyList<NormalizedCategoryInput> categories)
    {
        if (categories.Count == 0)
        {
            return "Add at least one category before saving.";
        }

        if (categories.Any(category => category.Name.Length == 0))
        {
            return "Each category must have a name.";
        }

        if (categories.Any(category => category.Name.Length > 120))
        {
            return "Category names must be 120 characters or fewer.";
        }

        if (categories.Any(category => category.WeightPercent <= 0m || category.WeightPercent > 100m))
        {
            return "Each category percentage must be greater than 0 and at most 100.";
        }

        decimal runningTotal = 0m;
        foreach (var category in categories)
        {
            if (runningTotal + category.WeightPercent > 100m)
            {
                return string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"Total cannot exceed 100%. Current total is {runningTotal:0.##}%, you tried to add {category.WeightPercent:0.##}%.");
            }

            runningTotal += category.WeightPercent;
        }

        if (categories.Any(category => category.Questions.Count == 0))
        {
            return "Each category must include at least one question.";
        }

        if (categories.SelectMany(category => category.Questions).Any(prompt => prompt.Length > 400))
        {
            return "Questions must be 400 characters or fewer.";
        }

        var totalWeight = runningTotal;
        if (Math.Abs(totalWeight - 100m) > 0.01m)
        {
            return "Category percentages must total exactly 100.";
        }

        return null;
    }

    private static string BuildPublishedNotificationMessage(string updatedByName, int versionNumber)
    {
        return BuildGenericEvaluationChangeNotificationMessage(updatedByName);
    }

    private static string BuildReuseNotificationMessage(string updatedByName, int versionNumber)
    {
        return BuildGenericEvaluationChangeNotificationMessage(updatedByName);
    }

    private static string BuildGenericEvaluationChangeNotificationMessage(string updatedByName)
    {
        var actorLabel = string.IsNullOrWhiteSpace(updatedByName) ? "QA Head" : updatedByName.Trim();
        return $"The QA head {actorLabel} has made a change to the QA evaluation. Please check and review.";
    }

    private async Task NotifyEvaluationChangeRecipientsAsync(
        string message,
        int templateId,
        CancellationToken cancellationToken)
    {
        await _notificationService.NotifyUsersByUserTypeAsync(
            QaAnalystRole,
            message,
            QaEvaluationNotificationCategory,
            orderId: templateId,
            cancellationToken);

        await _notificationService.NotifyUsersByUserTypeAsync(
            SupportAgentRole,
            message,
            QaEvaluationNotificationCategory,
            orderId: templateId,
            cancellationToken);
    }

    internal static QaEvaluationTemplateViewModel MapTemplate(
        QaEvaluationTemplate template,
        IReadOnlyDictionary<int, int>? scoresByQuestionId)
    {
        var categoryViewModels = template.Categories
            .OrderBy(category => category.DisplayOrder)
            .Select(category =>
            {
                var questions = category.Questions
                    .OrderBy(question => question.DisplayOrder)
                    .Select(question => new QaEvaluationQuestionViewModel(
                        question.QaEvaluationQuestionId,
                        question.QuestionKey,
                        question.Prompt,
                        question.DisplayOrder,
                        scoresByQuestionId is not null && scoresByQuestionId.TryGetValue(question.QaEvaluationQuestionId, out var score)
                            ? score
                            : null))
                    .ToList();

                var answeredScores = questions
                    .Where(question => question.Score.HasValue)
                    .Select(question => question.Score!.Value)
                    .ToList();
                var average = answeredScores.Count == 0
                    ? 0m
                    : Math.Round((decimal)answeredScores.Average(), 2, MidpointRounding.AwayFromZero);
                var weightedPoints = answeredScores.Count == 0
                    ? 0m
                    : Math.Round((average / 5m) * category.WeightPercent, 2, MidpointRounding.AwayFromZero);

                return new QaEvaluationCategoryViewModel(
                    category.QaEvaluationCategoryId,
                    category.Name,
                    category.WeightPercent,
                    category.DisplayOrder,
                    weightedPoints,
                    average,
                    questions);
            })
            .ToList();

        return new QaEvaluationTemplateViewModel(
            template.QaEvaluationTemplateId,
            template.VersionNumber,
            template.IsActive,
            categoryViewModels.Sum(category => category.Questions.Count),
            categoryViewModels.Sum(category => category.WeightPercent),
            template.CreatedById,
            template.UpdatedById,
            template.CreatedAtUtc.ToString("MMM dd, yyyy hh:mm tt"),
            template.UpdatedAtUtc.ToString("MMM dd, yyyy hh:mm tt"),
            template.ActivatedAtUtc?.ToString("MMM dd, yyyy hh:mm tt") ?? "Not activated",
            categoryViewModels);
    }

    private static string BuildQuestionKey(string categoryName, int categoryIndex, int questionIndex)
    {
        var slugSource = categoryName.ToLowerInvariant();
        var chars = new List<char>(slugSource.Length);

        foreach (var character in slugSource)
        {
            if (char.IsLetterOrDigit(character))
            {
                chars.Add(character);
                continue;
            }

            if (chars.Count == 0 || chars[^1] == '_')
            {
                continue;
            }

            chars.Add('_');
        }

        var slug = new string(chars.ToArray()).Trim('_');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"category_{categoryIndex}";
        }

        var raw = $"c{categoryIndex}_{slug}_q{questionIndex}";
        if (raw.Length <= 80)
        {
            return raw;
        }

        return raw[..80];
    }

    private sealed record NormalizedCategoryInput(
        string Name,
        decimal WeightPercent,
        List<string> Questions);
}
