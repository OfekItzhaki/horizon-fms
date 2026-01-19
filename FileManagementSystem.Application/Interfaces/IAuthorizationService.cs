namespace FileManagementSystem.Application.Interfaces;

public interface IAuthorizationService
{
    Task<bool> IsAuthorizedAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);
    Task<bool> HasRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
