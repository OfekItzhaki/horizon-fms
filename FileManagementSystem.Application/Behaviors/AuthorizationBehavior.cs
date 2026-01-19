using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;
    
    public AuthorizationBehavior(
        IAuthorizationService authorizationService,
        IAuthenticationService authenticationService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _authorizationService = authorizationService;
        _authenticationService = authenticationService;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var userId = await _authenticationService.GetCurrentUserIdAsync(cancellationToken);
        
        // For development: if no user authenticated, allow request (can be made stricter in production)
        // In production, uncomment the check below
        /*
        if (!userId.HasValue)
        {
            _logger.LogWarning("Unauthorized access attempt for {RequestType}", typeof(TRequest).Name);
            throw new UnauthorizedAccessException("User not authenticated");
        }
        */
        
        if (userId.HasValue)
        {
            // Determine required action based on request type
            var requestName = typeof(TRequest).Name;
            var action = requestName.Contains("Command") ? "Write" : "Read";
            var resource = "File";
            
            var isAuthorized = await _authorizationService.IsAuthorizedAsync(
                userId.Value,
                resource,
                action,
                cancellationToken);
            
            if (!isAuthorized)
            {
                _logger.LogWarning("User {UserId} not authorized for {RequestType}", userId.Value, typeof(TRequest).Name);
                throw new UnauthorizedAccessException($"User not authorized to perform {action} on {resource}");
            }
        }
        
        return await next();
    }
}
