using FluentValidation;
using System.Security.Claims;
using MemberTracker.Data;
using MemberTracker.Models;
using MemberTracker.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace NextHorizon.Controllers;

[ApiController]
[Authorize]
[Route("api/member-uploads")]
[Route("api/uploads")]
public class MemberUploadsController : ControllerBase
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MinReasonablePaceSecPerKm = 120;
    private const int MaxReasonablePaceSecPerKm = 1800;

    private readonly IMemberUploadRepository _memberUploadRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IValidator<CreateMemberUploadRequest> _createValidator;
    private readonly IValidator<UpdateMemberUploadRequest> _updateValidator;

    public MemberUploadsController(
        IMemberUploadRepository memberUploadRepository,
        IWebHostEnvironment webHostEnvironment,
        IValidator<CreateMemberUploadRequest> createValidator,
        IValidator<UpdateMemberUploadRequest> updateValidator)
    {
        _memberUploadRepository = memberUploadRepository;
        _webHostEnvironment = webHostEnvironment;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Authorize(Policy = UploadAuthorizationPolicies.ConsumerUpload)]
    [EnableRateLimiting("upload-write")]
    [ConditionalValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MemberUploadDto>> Create([FromForm] CreateMemberUploadRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var validationResult = await ValidateRequestAsync(_createValidator, request, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var savedProof = await SaveProofFileAsync(request.Proof!, cancellationToken);
        var roundedDistanceKm = ResolveAndRoundDistanceKm(request.DistanceKm, request.DistanceMi);
        if (!roundedDistanceKm.HasValue)
        {
            DeleteFileIfExists(savedProof.AbsolutePath);
            return BadRequest("Provide exactly one of DistanceKm or DistanceMi.");
        }

        try
        {
            var createdUpload = await _memberUploadRepository.CreateAsync(
                new CreateMemberUploadDbRequest(
                    userId.Value,
                    request.Title.Trim(),
                    request.ActivityName.Trim(),
                    request.ActivityDate.Date,
                    savedProof.ProofUrl,
                    roundedDistanceKm.Value,
                    request.MovingTimeSec,
                    request.Steps),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { uploadId = createdUpload.UploadId }, ToDto(createdUpload));
        }
        catch
        {
            DeleteFileIfExists(savedProof.AbsolutePath);
            throw;
        }
    }

    [HttpGet("{uploadId:int}")]
    public async Task<ActionResult<MemberUploadDto>> GetById(int uploadId, CancellationToken cancellationToken)
    {
        var upload = await _memberUploadRepository.GetByIdAsync(uploadId, cancellationToken);
        if (upload is null)
        {
            return NotFound();
        }

        return Ok(ToDto(upload));
    }

    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<MemberUploadDto>>> GetMyUploads([FromQuery] UploadListQuery query, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        return await GetPagedUploads(
            query,
            options => _memberUploadRepository.GetMyUploadsAsync(userId.Value, options, cancellationToken));
    }

    [HttpGet]
    [Authorize(Policy = UploadAuthorizationPolicies.ViewAllUploads)]
    public async Task<ActionResult<PagedResult<MemberUploadDto>>> GetAllUploads([FromQuery] UploadListQuery query, CancellationToken cancellationToken)
        => await GetPagedUploads(
            query,
            options => _memberUploadRepository.GetAllUploadsAsync(options, cancellationToken));

    [HttpPut("{uploadId:int}")]
    [EnableRateLimiting("upload-write")]
    [ConditionalValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MemberUploadDto>> Update(
        int uploadId,
        [FromForm] UpdateMemberUploadRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var validationResult = await ValidateRequestAsync(_updateValidator, request, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var existingUpload = await _memberUploadRepository.GetByIdAsync(uploadId, cancellationToken);
        if (existingUpload is null)
        {
            return NotFound();
        }

        if (!CanModifyUpload(existingUpload.UserId, userId.Value))
        {
            return Forbid();
        }

        (string AbsolutePath, string ProofUrl)? newProof = null;
        if (request.Proof is not null)
        {
            newProof = await SaveProofFileAsync(request.Proof, cancellationToken);
        }

        var previousProofUrl = existingUpload.ProofUrl;
        var roundedDistanceKm = ResolveAndRoundDistanceKm(request.DistanceKm, request.DistanceMi);
        if (!roundedDistanceKm.HasValue)
        {
            if (newProof is not null)
            {
                DeleteFileIfExists(newProof.Value.AbsolutePath);
            }

            return BadRequest("Provide exactly one of DistanceKm or DistanceMi.");
        }

        try
        {
            var updatedUpload = await _memberUploadRepository.UpdateAsync(
                new UpdateMemberUploadDbRequest(
                    uploadId,
                    userId.Value,
                    User.IsInRole(UploadRoles.Admin),
                    request.Title.Trim(),
                    request.ActivityName.Trim(),
                    request.ActivityDate.Date,
                    newProof?.ProofUrl,
                    roundedDistanceKm.Value,
                    request.MovingTimeSec,
                    request.Steps),
                cancellationToken);

            if (updatedUpload is null)
            {
                if (newProof is not null)
                {
                    DeleteFileIfExists(newProof.Value.AbsolutePath);
                }

                return NotFound();
            }

            if (newProof is not null && !string.Equals(previousProofUrl, updatedUpload.ProofUrl, StringComparison.OrdinalIgnoreCase))
            {
                DeleteFileIfExists(GetProofPathFromUrl(previousProofUrl));
            }

            return Ok(ToDto(updatedUpload));
        }
        catch
        {
            if (newProof is not null)
            {
                DeleteFileIfExists(newProof.Value.AbsolutePath);
            }

            throw;
        }
    }

    [HttpDelete("{uploadId:int}")]
    [EnableRateLimiting("upload-write")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int uploadId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var existingUpload = await _memberUploadRepository.GetByIdAsync(uploadId, cancellationToken);
        if (existingUpload is null)
        {
            return NotFound();
        }

        if (!CanModifyUpload(existingUpload.UserId, userId.Value))
        {
            return Forbid();
        }

        var deleted = await _memberUploadRepository.DeleteAsync(
            new DeleteMemberUploadDbRequest(uploadId, userId.Value, User.IsInRole(UploadRoles.Admin)),
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        DeleteFileIfExists(GetProofPathFromUrl(existingUpload.ProofUrl));
        return NoContent();
    }

    private async Task<ActionResult<PagedResult<MemberUploadDto>>> GetPagedUploads(
        UploadListQuery listQuery,
        Func<UploadQueryOptions, Task<PagedResult<MemberUpload>>> fetchPage)
    {
        var normalizedQuery = NormalizeQuery(listQuery);
        if (normalizedQuery is null)
        {
            return BadRequest("Sort must be one of: createdAt_desc, activityDate_desc, longestDistance, bestPace.");
        }

        var pageResult = await fetchPage(normalizedQuery);

        return Ok(new PagedResult<MemberUploadDto>
        {
            PageNumber = pageResult.PageNumber,
            PageSize = pageResult.PageSize,
            TotalCount = pageResult.TotalCount,
            Items = pageResult.Items.Select(ToDto).ToList(),
        });
    }

    private static UploadQueryOptions? NormalizeQuery(UploadListQuery query)
    {
        var pageNumber = query.Page.GetValueOrDefault(query.PageNumber.GetValueOrDefault(DefaultPageNumber));
        if (pageNumber <= 0)
        {
            pageNumber = DefaultPageNumber;
        }

        var pageSize = query.PageSize.GetValueOrDefault(DefaultPageSize);
        if (pageSize <= 0)
        {
            pageSize = DefaultPageSize;
        }

        pageSize = Math.Min(pageSize, MaxPageSize);

        var rawSort = !string.IsNullOrWhiteSpace(query.Sort)
            ? query.Sort
            : query.SortBy;

        var normalizedSort = NormalizeSort(rawSort);
        return normalizedSort is null
            ? null
            : new UploadQueryOptions(pageNumber, pageSize, normalizedSort);
    }

    private static string? NormalizeSort(string? sortBy)
    {
        var sort = string.IsNullOrWhiteSpace(sortBy)
            ? "createdat_desc"
            : sortBy.Trim().ToLowerInvariant();

        return sort switch
        {
            "latest" or "createdat_desc" or "created_at_desc" => "createdAt_desc",
            "activitydate_desc" or "activity_date_desc" => "activityDate_desc",
            "longestdistance" or "distance" or "distance_desc" => "longestDistance",
            "bestpace" or "pace" or "pace_asc" => "bestPace",
            _ => null,
        };
    }

    private async Task<(string AbsolutePath, string ProofUrl)> SaveProofFileAsync(IFormFile proof, CancellationToken cancellationToken)
    {
        var uploadsDirectory = GetProofUploadsDirectory();
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(proof.FileName).ToLowerInvariant();
        var proofFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDirectory, proofFileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await proof.CopyToAsync(stream, cancellationToken);
        }

        return (filePath, $"/uploads/proofs/{proofFileName}");
    }

    private string GetProofUploadsDirectory()
    {
        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        }

        return Path.Combine(webRoot, "uploads", "proofs");
    }

    private string? GetProofPathFromUrl(string? proofUrl)
    {
        if (string.IsNullOrWhiteSpace(proofUrl) || !proofUrl.StartsWith("/uploads/proofs/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fileName = Path.GetFileName(proofUrl);
        return string.IsNullOrWhiteSpace(fileName) ? null : Path.Combine(GetProofUploadsDirectory(), fileName);
    }

    private static void DeleteFileIfExists(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : null;
    }

    private bool CanModifyUpload(int ownerUserId, int currentUserId)
        => ownerUserId == currentUserId || User.IsInRole(UploadRoles.Admin);

    private static bool IsPaceSuspicious(int? avgPaceSecPerKm)
    {
        if (!avgPaceSecPerKm.HasValue)
        {
            return false;
        }

        return avgPaceSecPerKm.Value < MinReasonablePaceSecPerKm || avgPaceSecPerKm.Value > MaxReasonablePaceSecPerKm;
    }

    private static decimal? ResolveAndRoundDistanceKm(decimal? distanceKm, decimal? distanceMi)
    {
        var resolvedDistanceKm = UploadValidationRules.ResolveDistanceKm(distanceKm, distanceMi);
        return resolvedDistanceKm.HasValue
            ? decimal.Round(resolvedDistanceKm.Value, 2, MidpointRounding.AwayFromZero)
            : null;
    }

    private static decimal ToDistanceMi(decimal distanceKm)
        => decimal.Round(UploadValidationRules.ConvertKmToMiles(distanceKm), 2, MidpointRounding.AwayFromZero);

    private static int? ToAvgPaceSecPerMi(int movingTimeSec, decimal distanceKm)
    {
        var distanceMi = UploadValidationRules.ConvertKmToMiles(distanceKm);
        return distanceMi > 0
            ? (int)Math.Round(movingTimeSec / (double)distanceMi, MidpointRounding.AwayFromZero)
            : null;
    }

    private static MemberUploadDto ToDto(MemberUpload upload)
        => new()
        {
            UploadId = upload.UploadId,
            UserId = upload.UserId.ToString(),
            Title = upload.Title,
            ActivityName = upload.ActivityName,
            ActivityDate = upload.ActivityDate,
            ProofUrl = upload.ProofUrl,
            DistanceKm = upload.DistanceKm,
            DistanceMi = ToDistanceMi(upload.DistanceKm),
            MovingTimeSec = upload.MovingTimeSec,
            Steps = upload.Steps,
            AvgPaceSecPerKm = upload.AvgPaceSecPerKm,
            AvgPaceSecPerMi = ToAvgPaceSecPerMi(upload.MovingTimeSec, upload.DistanceKm),
            IsPaceSuspicious = IsPaceSuspicious(upload.AvgPaceSecPerKm),
            CreatedAt = upload.CreatedAt,
            UpdatedAt = upload.UpdatedAt,
        };

    private ActionResult? BuildValidationProblem(FluentValidation.Results.ValidationResult validationResult)
    {
        if (validationResult.IsValid)
        {
            return null;
        }

        var errors = validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
        });
    }

    private async Task<ActionResult?> ValidateRequestAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        return BuildValidationProblem(validationResult);
    }
}
