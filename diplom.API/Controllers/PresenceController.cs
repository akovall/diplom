using diplom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PresenceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PresenceController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // POST: api/presence/heartbeat
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return NotFound();

            if (!user.IsActive)
                return Forbid();

            user.LastSeenUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }

        // POST: api/presence/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return NotFound();

            user.LastSeenUtc = null;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }
    }
}
