using FluentValidation;
using FileManagementSystem.Application.Commands;

namespace FileManagementSystem.Application.Validators;

public class AddTagsCommandValidator : AbstractValidator<AddTagsCommand>
{
    public AddTagsCommandValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty().WithMessage("File ID is required");
        
        RuleFor(x => x.Tags)
            .NotEmpty().WithMessage("At least one tag is required")
            .Must(tags => tags != null && tags.All(t => !string.IsNullOrWhiteSpace(t)))
            .WithMessage("All tags must be non-empty");
    }
}
