using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
    private Guid? _currentUserId;
    
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
        var jwtSecret = _configuration["JWT_SECRET"] ?? "your_super_secret_key_at_least_32_chars_long_horizon_fms_2026";
        var jwtIssuer = _configuration["JWT_ISSUER"] ?? "FileManagementSystem";
        var jwtAudience = _configuration["JWT_AUDIENCE"] ?? "FileManagementSystem";
        
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Name, username)
        };
        
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogInformation("JWT token generated for user: {Username}", username);
        
        return Task.FromResult(tokenString);
    }
    
    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var jwtSecret = _configuration["JWT_SECRET"] ?? "your_super_secret_key_at_least_32_chars_long_horizon_fms_2026";
            var jwtIssuer = _configuration["JWT_ISSUER"] ?? "FileManagementSystem";
            var jwtAudience = _configuration["JWT_AUDIENCE"] ?? "FileManagementSystem";
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
            
            tokenHandler.ValidateToken(token, validationParameters, out _);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
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
