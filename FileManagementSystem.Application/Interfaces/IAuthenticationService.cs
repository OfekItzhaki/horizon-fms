namespace FileManagementSystem.Application.Interfaces;

public interface IAuthenticationService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<string> GenerateTokenAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
}
