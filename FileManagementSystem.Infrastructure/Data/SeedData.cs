using System.Security.Cryptography;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Infrastructure.Services;

namespace FileManagementSystem.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (context.Set<User>().Any())
        {
            return; // Already seeded
        }
        
        // Create default admin user
        var adminPassword = "Admin@123"; // In production, use secure password generation
        var adminSalt = AuthenticationService.GenerateSalt();
        var adminHash = HashPassword(adminPassword, adminSalt);
        
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@filemanager.local",
            PasswordHash = adminHash,
            Salt = adminSalt,
            Roles = new List<string> { "Admin", "User", "Editor" },
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        
        context.Set<User>().Add(adminUser);
        
        // Create default user
        var userPassword = "User@123";
        var userSalt = AuthenticationService.GenerateSalt();
        var userHash = HashPassword(userPassword, userSalt);
        
        var regularUser = new User
        {
            Username = "user",
            Email = "user@filemanager.local",
            PasswordHash = userHash,
            Salt = userSalt,
            Roles = new List<string> { "User", "Viewer" },
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        
        context.Set<User>().Add(regularUser);
        
        // Helper method to hash password (same as AuthenticationService)
        static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }
        
        await context.SaveChangesAsync();
    }
}
