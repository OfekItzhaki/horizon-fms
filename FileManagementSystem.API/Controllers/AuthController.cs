using Asp.Versioning;
using FileManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var isAuthenticated = await _authService.AuthenticateAsync(request.Username, request.Password);
        
        if (!isAuthenticated)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tokens = await _authService.GenerateTokensAsync(request.Username, ipAddress);

        // Set refresh token in HTTP-only cookie for security
        SetRefreshTokenCookie(tokens.RefreshToken);

        return Ok(new
        {
            accessToken = tokens.AccessToken,
            message = "Login successful"
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token not found" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tokens = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        if (tokens == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Set new refresh token in cookie
        SetRefreshTokenCookie(tokens.RefreshToken);

        return Ok(new
        {
            accessToken = tokens.AccessToken,
            message = "Token refreshed successfully"
        });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token not found" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var success = await _authService.RevokeTokenAsync(refreshToken, ipAddress);

        if (!success)
        {
            return BadRequest(new { message = "Token revocation failed" });
        }

        // Clear the cookie
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Token revoked successfully" });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(30),
            Secure = true, // HTTPS only
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}

public record LoginRequest(string Username, string Password);
