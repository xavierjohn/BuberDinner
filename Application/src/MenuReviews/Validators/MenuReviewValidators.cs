namespace BuberDinner.Application.MenuReviews.Validators;

using BuberDinner.Application.MenuReviews.Commands;
using FluentValidation;

/// <summary>
/// Validates <see cref="SubmitMenuReviewCommand"/> at the Trellis Mediator
/// <c>ValidationBehavior</c> stage — runs BEFORE the handler, short-circuits to
/// 422 Problem Details on failure with one field violation per offending property.
/// Wired by <c>services.AddTrellisFluentValidation(typeof(SubmitMenuReviewCommandValidator).Assembly)</c>.
/// </summary>
public sealed class SubmitMenuReviewCommandValidator : AbstractValidator<SubmitMenuReviewCommand>
{
    public SubmitMenuReviewCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("Comment is required.")
            .MaximumLength(1000)
            .WithMessage("Comment must not exceed 1000 characters.");
    }
}

public sealed class UpdateMenuReviewCommandValidator : AbstractValidator<UpdateMenuReviewCommand>
{
    public UpdateMenuReviewCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("Comment is required.")
            .MaximumLength(1000)
            .WithMessage("Comment must not exceed 1000 characters.");
    }
}
