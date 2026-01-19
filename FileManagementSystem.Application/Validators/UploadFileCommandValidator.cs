using FluentValidation;
using FileManagementSystem.Application.Commands;

namespace FileManagementSystem.Application.Validators;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    private const long MaxFileSize = 10L * 1024 * 1024 * 1024; // 10GB
    
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.SourcePath)
            .NotEmpty().WithMessage("Source path is required")
            .Must(path => File.Exists(path))
            .WithMessage("Source file must exist");
        
        RuleFor(x => x)
            .Must(cmd => 
            {
                if (!File.Exists(cmd.SourcePath)) return false;
                var fileInfo = new FileInfo(cmd.SourcePath);
                return fileInfo.Length <= MaxFileSize;
            })
            .WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024 * 1024)}GB");
    }
}
