using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Infrastructure.Data;
using FileManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManagementSystem.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private static Guid? _currentUserId;
    
    public AuthenticationService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuthenticationService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Authentication failed: User not found - {Username}", username);
            return false;
        }
        
        var passwordHash = HashPassword(password, user.Salt);
        
        if (passwordHash != user.PasswordHash)
        {
            _logger.LogWarning("Authentication failed: Invalid password for user - {Username}", username);
            return false;
        }
        
        user.LastLoginDate = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        
        _currentUserId = user.Id;
        _logger.LogInformation("User authenticated successfully: {Username}", username);
        
        return true;
    }
    
    public Task<string> GenerateTokenAsync(string username, CancellationToken cancellationToken = default)
    {
        // For desktop app, use simple token generation
        // In production, use JWT or similar
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{DateTime.UtcNow:O}"));
        return Task.FromResult(token);
    }
    
    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Simple validation for desktop app
        // In production, validate JWT properly
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            return Task.FromResult(decoded.Contains(':'));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentUserId);
    }
    
    private static string HashPassword(string password, string? salt)
    {
        var saltBytes = string.IsNullOrEmpty(salt) 
            ? RandomNumberGenerator.GetBytes(16) 
            : Convert.FromBase64String(salt);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return Convert.ToBase64String(hash);
    }
    
    public static string GenerateSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(salt);
    }
}
