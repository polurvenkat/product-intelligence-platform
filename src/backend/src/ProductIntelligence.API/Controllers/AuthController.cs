using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.DTOs.Auth;
using ProductIntelligence.Application.Services;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Infrastructure.Repositories;

namespace ProductIntelligence.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters" });
        }

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "User with this email already exists" });
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower(),
            Name = request.Name,
            Company = request.Company,
            PasswordHash = _authService.HashPassword(request.Password),
            Tier = Core.Enums.CustomerTier.Starter,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            RefreshToken = _authService.GenerateRefreshToken(),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _userRepository.CreateAsync(user);

        _logger.LogInformation("New user registered: {Email}", user.Email);

        // Generate tokens
        var accessToken = _authService.GenerateAccessToken(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken!,
            ExpiresIn = 3600, // 1 hour
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Company = user.Company,
                Tier = user.Tier.ToString(),
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        // Find user
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Verify password
        if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Update last login and refresh token
        user.LastLoginAt = DateTime.UtcNow;
        user.RefreshToken = _authService.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        // Generate access token
        var accessToken = _authService.GenerateAccessToken(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken!,
            ExpiresIn = 3600,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Company = user.Company,
                Tier = user.Tier.ToString(),
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // Find user by refresh token
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Generate new tokens
        var accessToken = _authService.GenerateAccessToken(user);
        user.RefreshToken = _authService.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken!,
            ExpiresIn = 3600,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Company = user.Company,
                Tier = user.Tier.ToString(),
                CreatedAt = user.CreatedAt
            }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && Guid.TryParse(userId, out var id))
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiresAt = null;
                await _userRepository.UpdateAsync(user);
                _logger.LogInformation("User logged out: {Email}", user.Email);
            }
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Company = user.Company,
            Tier = user.Tier.ToString(),
            CreatedAt = user.CreatedAt
        });
    }
}
