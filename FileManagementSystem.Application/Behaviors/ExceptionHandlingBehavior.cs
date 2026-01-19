using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FluentValidation;
using FileManagementSystem.Domain.Exceptions;
using FileManagementSystem.Domain.Exceptions;

namespace FileManagementSystem.Application.Behaviors;

public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;
    
    public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (FileDuplicateException ex)
        {
            _logger.LogInformation(ex, "Duplicate file detected in {RequestType}: {FilePath}",
                typeof(TRequest).Name, ex.FilePath);
            throw; // Re-throw to be handled by middleware
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception in {RequestType}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception in {RequestType}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception in {RequestType}",
                typeof(TRequest).Name);
            throw;
        }
    }
}
