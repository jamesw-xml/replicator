using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using replicator.Settings;

namespace replicator.HttpApi;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly string? _username;
    private readonly string? _password;
    private readonly string? _jwtSecret;
    private readonly bool _enableAuth;

    public AuthController(Replicator settings)
    {
        _enableAuth = settings.EnableAuth;
        if (!settings.EnableAuth)
        {
            return;
        }
        _username = settings.Auth?.Username;
        _password = settings.Auth?.Password;
        _jwtSecret = settings.Auth?.JwtSecret;
    }

    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            return StatusCode(500, "Auth not configured");
        if (req.Username == _username && req.Password == _password)
        {
            var token = GenerateJwtToken(req.Username);
            return Ok(new LoginResponse(token));
        }
        return Unauthorized();
    }

    [HttpGet("status")]
    public IActionResult Status() {
        return Ok(new { enabled = _enableAuth });
    }

    private string GenerateJwtToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
