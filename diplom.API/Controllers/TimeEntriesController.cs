using diplom.Data;
using diplom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using diplom.API.Hubs;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TimeEntriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TimeHub> _hub;

        public TimeEntriesController(AppDbContext context, IHubContext<TimeHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: api/timeentries/today
        [HttpGet("today")]
        public async Task<ActionResult<List<TimeEntry>>> GetTodayEntries()
        {
            var userId = GetCurrentUserId();
            var startUtc = DateTime.UtcNow.Date;
            var endUtc = startUtc.AddDays(1);

            var entries = await _context.TimeLogs
                .Include(e => e.Task)
                .Where(e => e.UserId == userId)
                // Use UTC day window to avoid timezone/date truncation issues in DB
                .Where(e => e.StartTime < endUtc && (e.EndTime ?? DateTime.UtcNow) >= startUtc)
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();

            return Ok(entries);
        }

        // GET: api/timeentries/task/5
        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<List<TimeEntry>>> GetByTask(int taskId)
        {
            var userId = GetCurrentUserId();

            return await _context.TimeLogs
                .Where(e => e.TaskId == taskId && e.UserId == userId)
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();
        }

        // GET: api/timeentries/active — open entry for current user (if any)
        [HttpGet("active")]
        public async Task<ActionResult<TimeEntry>> GetActive()
        {
            var userId = GetCurrentUserId();
            var entry = await _context.TimeLogs
                .Include(e => e.Task)
                .Where(e => e.UserId == userId && e.EndTime == null)
                .OrderByDescending(e => e.StartTime)
                .FirstOrDefaultAsync();

            if (entry == null)
                return NotFound();

            return Ok(entry);
        }

        // POST: api/timeentries/stop-active — close current open entry
        [HttpPost("stop-active")]
        public async Task<ActionResult<TimeEntry>> StopActive([FromBody] StopActiveRequest request)
        {
            var userId = GetCurrentUserId();
            var entry = await _context.TimeLogs
                .Where(e => e.UserId == userId && e.EndTime == null)
                .OrderByDescending(e => e.StartTime)
                .FirstOrDefaultAsync();

            if (entry == null)
                return NotFound();

            entry.EndTime = DateTime.UtcNow;
            entry.Comment = request?.Comment ?? entry.Comment;
            entry.IsManual = false;

            await _context.SaveChangesAsync();

            var created = await _context.TimeLogs
                .Include(e => e.Task)
                .FirstOrDefaultAsync(e => e.Id == entry.Id);

            await _hub.Clients.All.SendAsync("TimeEntryChanged", entry.TaskId, userId);

            return Ok(created);
        }

        // POST: api/timeentries
        [HttpPost]
        public async Task<ActionResult<TimeEntry>> Create([FromBody] TimeEntry entry)
        {
            var userId = GetCurrentUserId();

            var task = await _context.Tasks.FindAsync(entry.TaskId);
            if (task == null)
                return BadRequest(new { message = "Task not found" });

            if (task.AssigneeId != userId)
                return Forbid();

            // Always set UserId from JWT token (not from request body)
            entry.UserId = userId;

            if (entry.StartTime == default)
                entry.StartTime = DateTime.UtcNow;

            if (entry.EndTime.HasValue && entry.EndTime.Value < entry.StartTime)
                return BadRequest(new { message = "EndTime cannot be earlier than StartTime" });

            // Do not allow multiple open entries per user
            if (!entry.EndTime.HasValue)
            {
                var hasOpen = await _context.TimeLogs.AnyAsync(e => e.UserId == userId && e.EndTime == null, HttpContext.RequestAborted);
                if (hasOpen)
                    return Conflict(new { message = "There is already an active timer session." });
            }

            _context.TimeLogs.Add(entry);
            await _context.SaveChangesAsync();

            var created = await _context.TimeLogs
                .Include(e => e.Task)
                .FirstOrDefaultAsync(e => e.Id == entry.Id);

            await _hub.Clients.All.SendAsync("TimeEntryChanged", entry.TaskId, userId);

            return Ok(created);
        }

        // PUT: api/timeentries/5
        [HttpPut("{id}")]
        public async Task<ActionResult<TimeEntry>> Update(int id, [FromBody] TimeEntry entry)
        {
            var userId = GetCurrentUserId();
            var existing = await _context.TimeLogs.FindAsync(id);

            if (existing == null)
                return NotFound();

            // Only allow editing own entries
            if (existing.UserId != userId)
                return Forbid();

            existing.EndTime = entry.EndTime;
            existing.Comment = entry.Comment;
            existing.IsManual = entry.IsManual;

            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("TimeEntryChanged", existing.TaskId, userId);
            return Ok(existing);
        }
    }

    public sealed class StopActiveRequest
    {
        public string? Comment { get; set; }
    }
}
