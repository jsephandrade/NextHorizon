using FluentValidation;
using NextHorizon.Validation;

namespace NextHorizon.Modules.MemberTracker.Models;

public class CreateMemberUploadRequestValidator : AbstractValidator<CreateMemberUploadRequest>
{
    public CreateMemberUploadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(x => x.ActivityName)
            .NotEmpty()
            .Must(UploadValidationRules.BeAllowedActivity)
            .WithMessage("ActivityName must be one of: Run, Walk, Ride, Training.");

        RuleFor(x => x.ActivityDate)
            .NotEmpty()
            .Must(UploadValidationRules.BeNotMoreThanOneDayAhead)
            .WithMessage("ActivityDate cannot be more than 1 day in the future.");

        RuleFor(x => x.DistanceKm)
            .GreaterThan(0)
            .LessThanOrEqualTo(UploadValidationRules.MaxDistanceKm)
            .When(x => x.DistanceKm.HasValue);

        RuleFor(x => x.DistanceMi)
            .GreaterThan(0)
            .LessThanOrEqualTo(UploadValidationRules.MaxDistanceMi)
            .When(x => x.DistanceMi.HasValue);

        RuleFor(x => x)
            .Must(HaveExactlyOneDistanceValue)
            .WithMessage("Provide exactly one of DistanceKm or DistanceMi.");

        RuleFor(x => x.MovingTimeSec)
            .InclusiveBetween(UploadValidationRules.MinMovingTimeSec, UploadValidationRules.MaxMovingTimeSec);

        RuleFor(x => x.Steps)
            .InclusiveBetween(UploadValidationRules.MinSteps, UploadValidationRules.MaxSteps)
            .When(x => x.Steps.HasValue);

        RuleFor(x => x)
            .Must(HaveConsistentDistanceAndTime)
            .WithMessage("Distance and MovingTimeSec must both be greater than zero.");

        RuleFor(x => x.Proof)
            .NotNull()
            .WithMessage("Proof image is required.");

        When(x => x.Proof is not null, () =>
        {
            RuleFor(x => x.Proof!.Length)
                .GreaterThan(0)
                .LessThanOrEqualTo(UploadValidationRules.MaxProofSizeBytes)
                .WithMessage("Proof image must be 5MB or smaller.");

            RuleFor(x => x.Proof!)
                .Must(UploadValidationRules.HaveAllowedExtension)
                .WithMessage("Proof image must be jpg, jpeg, png, or webp.");

            RuleFor(x => x.Proof!)
                .Must(UploadValidationRules.HaveAllowedContentType)
                .WithMessage("Proof image content type must be image/jpeg, image/png, or image/webp.");

            RuleFor(x => x.Proof!)
                .Must(UploadValidationRules.HaveMatchingExtensionAndContentType)
                .WithMessage("Proof image extension and content type must match.");

            RuleFor(x => x.Proof!)
                .Must(UploadValidationRules.HaveMatchingSignature)
                .WithMessage("Proof image signature does not match file type.");

            RuleFor(x => x.Proof!)
                .Must(UploadValidationRules.HaveValidImageStructure)
                .WithMessage("Proof image appears malformed or corrupted.");
        });
    }

    private static bool HaveExactlyOneDistanceValue(CreateMemberUploadRequest request)
        => request.DistanceKm.HasValue ^ request.DistanceMi.HasValue;

    private static bool HaveConsistentDistanceAndTime(CreateMemberUploadRequest request)
    {
        var distanceKm = UploadValidationRules.ResolveDistanceKm(request.DistanceKm, request.DistanceMi);
        return distanceKm.HasValue && UploadValidationRules.HaveConsistentDistanceAndTime(distanceKm.Value, request.MovingTimeSec);
    }
}

