using FluentValidation;
using FileManagementSystem.Application.Commands;

namespace FileManagementSystem.Application.Validators;

public class RenameFileCommandValidator : AbstractValidator<RenameFileCommand>
{
    public RenameFileCommandValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty().WithMessage("File ID is required");
        
        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("New name cannot be empty")
            .Must(name => !name.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
            .WithMessage("New name contains invalid characters");
    }
}
