namespace FileManagementSystem.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string Username { get; set; }
    
    public required string Email { get; set; }
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? Salt { get; set; }
    
    public List<string> Roles { get; set; } = new();
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}
