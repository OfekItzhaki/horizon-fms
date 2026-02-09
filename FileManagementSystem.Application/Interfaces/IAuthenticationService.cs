using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Application.Interfaces;

public record AuthTokens(string AccessToken, string RefreshToken);

public interface IAuthenticationService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<string> GenerateTokenAsync(string username, CancellationToken cancellationToken = default);
    Task<AuthTokens> GenerateTokensAsync(string username, string ipAddress, CancellationToken cancellationToken = default);
    Task<AuthTokens?> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
}
