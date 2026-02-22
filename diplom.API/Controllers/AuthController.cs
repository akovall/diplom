using diplom.API.DTOs;
using diplom.Data;
using diplom.Models;
using diplom.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace diplom.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static readonly string[] DefaultProfessionsUa =
        {
            "Розробник",
            "Інженер-програміст",
            "Інженер з якості (QA)",
            "UI/UX дизайнер",
            "Менеджер",
            "Системний адміністратор"
        };

        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Conflict(new { message = "Username already exists" });

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                JobTitle = NormalizeProfession(request.JobTitle),
                Role = UserRole.Employee,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                Role = user.Role.ToString()
            });
        }

        // GET: api/auth/professions
        [HttpGet("professions")]
        [AllowAnonymous]
        public ActionResult<List<string>> GetProfessions()
        {
            var result = DefaultProfessionsUa
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();

            return Ok(result);
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username or password" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is deactivated" });

            user.LastSeenUtc = DateTime.UtcNow;
            user.CurrentSessionId = Guid.NewGuid();

            // Safety: if app was closed/crashed while tracking, close any open sessions.
            var openEntries = await _context.TimeLogs
                .Where(e => e.UserId == user.Id && e.EndTime == null)
                .ToListAsync();
            foreach (var e in openEntries)
            {
                e.EndTime = DateTime.UtcNow;
                e.Comment = string.IsNullOrWhiteSpace(e.Comment)
                    ? "Auto-stopped on login"
                    : $"{e.Comment} (auto-stopped on login)";
            }

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                Role = user.Role.ToString()
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.JobTitle,
                Role = user.Role.ToString(),
                user.IsActive
            });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("fullName", user.FullName),
                // Include both to avoid inbound claim type mapping surprises.
                new Claim("sid", user.CurrentSessionId?.ToString() ?? string.Empty),
                new Claim(ClaimTypes.Sid, user.CurrentSessionId?.ToString() ?? string.Empty)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string NormalizeProfession(string? profession)
        {
            var value = (profession ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.ToLowerInvariant() switch
            {
                "developer" => "Розробник",
                "software engineer" => "Інженер-програміст",
                "qa" => "Інженер з якості (QA)",
                "qa engineer" => "Інженер з якості (QA)",
                "quality assurance" => "Інженер з якості (QA)",
                "ui ux designer" => "UI/UX дизайнер",
                "ui/ux designer" => "UI/UX дизайнер",
                "manager" => "Менеджер",
                "system admin" => "Системний адміністратор",
                "system administrator" => "Системний адміністратор",
                _ => value
            };
        }
    }
}
