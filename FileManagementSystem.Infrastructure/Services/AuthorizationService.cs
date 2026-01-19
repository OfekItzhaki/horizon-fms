using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Infrastructure.Data;
using FileManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManagementSystem.Infrastructure.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuthorizationService> _logger;
    
    public AuthorizationService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuthorizationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    public async Task<bool> IsAuthorizedAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Authorization failed: User not found or inactive - {UserId}", userId);
            return false;
        }
        
        // Simple role-based authorization
        // Admin can do everything
        if (user.Roles.Contains("Admin"))
        {
            return true;
        }
        
        // User can read, but need specific roles for write operations
        if (action == "Read")
        {
            return user.Roles.Contains("User") || user.Roles.Contains("Viewer");
        }
        
        if (action == "Write" || action == "Delete")
        {
            return user.Roles.Contains("User") || user.Roles.Contains("Editor");
        }
        
        return false;
    }
    
    public async Task<bool> HasRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        return user != null && user.IsActive && user.Roles.Contains(role);
    }
}
