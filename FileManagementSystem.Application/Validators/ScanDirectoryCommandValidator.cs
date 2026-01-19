using FluentValidation;
using FileManagementSystem.Application.Commands;

namespace FileManagementSystem.Application.Validators;

public class ScanDirectoryCommandValidator : AbstractValidator<ScanDirectoryCommand>
{
    public ScanDirectoryCommandValidator()
    {
        RuleFor(x => x.DirectoryPath)
            .NotEmpty().WithMessage("Directory path is required")
            .Must(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .WithMessage("Directory path must exist and be accessible");
    }
}
