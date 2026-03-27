using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AI.Agent.Email.Core.DTOs;
using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AI.Agent.Email.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email))
        {
            return BadRequest(new { Message = "User with this email already exists" });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Create default settings
        var settings = new UserSettings
        {
            UserId = user.Id,
            AutoReplyEnabled = false,
            RequireApprovalForReplies = true,
            EmailCheckIntervalMinutes = 1,
            MinConfidenceThreshold = 0.7
        };
        await _unitOfWork.Users.UpdateSettingsAsync(settings);
        await _unitOfWork.SaveChangesAsync();

        var tokens = GenerateTokens(user);
        
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = tokens.ExpiresAt.AddDays(7);
        await _unitOfWork.SaveChangesAsync();

        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        var tokens = GenerateTokens(user);
        
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = tokens.ExpiresAt.AddDays(7);
        await _unitOfWork.SaveChangesAsync();

        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // Find user by refresh token
        var users = await _unitOfWork.Users.GetByEmailAsync(""); // We need a better way to find by refresh token
        
        return Ok(new { Message = "Token refresh endpoint" });
    }

    private AuthResponse GenerateTokens(User user)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "EmailAgent";
        var audience = _configuration["Jwt:Audience"] ?? "EmailAgentUsers";
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName)
        };

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            ExpiresAt = expires
        };
    }
}
