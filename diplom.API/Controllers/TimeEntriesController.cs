using diplom.Data;
using diplom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TimeEntriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimeEntriesController(AppDbContext context)
        {
            _context = context;
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
            var today = DateTime.Today;

            var entries = await _context.TimeLogs
                .Include(e => e.Task)
                .Where(e => e.UserId == userId)
                .Where(e => e.StartTime.Date == today || (e.EndTime.HasValue && e.EndTime.Value.Date == today))
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
            return Ok(existing);
        }
    }
}
