using diplom.Data;
using diplom.API.DTOs;
using diplom.API.Services;
using diplom.Models;
using diplom.Models.Analytics;
using diplom.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplom.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserActivityService _userActivityService;

        public UsersController(AppDbContext context, IUserActivityService userActivityService)
        {
            _context = context;
            _userActivityService = userActivityService;
        }

        // GET: api/users — admin only
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.JobTitle,
                    Role = u.Role.ToString(),
                    u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/assignable — for Manager/Admin to populate assignee dropdown
        [HttpGet("assignable")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAssignable()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.JobTitle, Role = u.Role.ToString() })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/users/5/role
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] UserRole newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/users/5/deactivate
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/users/5/activate
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: api/users/activity — Admin/Manager: 3-state user activity (offline/idle/active)
        [HttpGet("activity")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<List<UserActivityDto>>> GetUsersActivity(CancellationToken cancellationToken)
        {
            var result = await _userActivityService.GetUsersActivityAsync(cancellationToken);
            return Ok(result);
        }

        // GET: api/users/{id}/analytics?days=14
        // Admin/Manager: per-user charts + summary for time/task analysis.
        [HttpGet("{id}/analytics")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<UserAnalyticsDto>> GetUserAnalytics(
            int id,
            [FromQuery] int days = 14,
            CancellationToken cancellationToken = default)
        {
            if (days < 7) days = 7;
            if (days > 90) days = 90;

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new { u.Id, u.FullName, u.JobTitle })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                return NotFound();

            var toUtc = DateTime.UtcNow;
            var fromUtc = toUtc.Date.AddDays(-days + 1);

            // Load tasks with timestamps for assignment/completion and time entries for actual hours.
            var tasks = await _context.Tasks
                .Include(t => t.TimeEntries)
                .Where(t => t.AssigneeId == id)
                .Where(t =>
                    (t.AssignedAtUtc.HasValue && t.AssignedAtUtc.Value >= fromUtc && t.AssignedAtUtc.Value <= toUtc) ||
                    (t.CompletedAtUtc.HasValue && t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc))
                .ToListAsync(cancellationToken);

            var logs = await _context.TimeLogs
                .Where(l => l.UserId == id)
                .Where(l => l.EndTime.HasValue)
                .Where(l => l.EndTime!.Value >= fromUtc && l.EndTime!.Value <= toUtc)
                .ToListAsync(cancellationToken);

            var totalWorkedHours = Math.Round(TimeSpan.FromTicks(logs.Sum(l => l.Duration.Ticks)).TotalHours, 2);

            var assigned = tasks.Count(t => t.AssignedAtUtc.HasValue && t.AssignedAtUtc.Value >= fromUtc && t.AssignedAtUtc.Value <= toUtc);
            var completed = tasks.Count(t => t.CompletedAtUtc.HasValue && t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc);
            var completedFromAssignedInPeriod = tasks.Count(t =>
                t.AssignedAtUtc.HasValue &&
                t.AssignedAtUtc.Value >= fromUtc && t.AssignedAtUtc.Value <= toUtc &&
                t.CompletedAtUtc.HasValue &&
                t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc);
            var overdueCompleted = tasks.Count(t =>
                t.CompletedAtUtc.HasValue &&
                t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc &&
                t.Deadline.HasValue &&
                t.CompletedAtUtc.Value > t.Deadline.Value);

            // Daily buckets in UTC (client can label in local time if needed).
            var daysList = Enumerable.Range(0, days)
                .Select(i => fromUtc.Date.AddDays(i))
                .Select(day => new UserAnalyticsDayDto
                {
                    DayUtc = day,
                    WorkedHours = 0,
                    TasksAssigned = 0,
                    TasksCompleted = 0,
                    OverdueCompleted = 0
                })
                .ToList();

            var dayByDate = daysList.ToDictionary(d => d.DayUtc.Date, d => d);

            foreach (var log in logs)
            {
                var day = log.EndTime!.Value.Date;
                if (!dayByDate.TryGetValue(day, out var bucket))
                    continue;

                bucket.WorkedHours = Math.Round(bucket.WorkedHours + TimeSpan.FromTicks(log.Duration.Ticks).TotalHours, 2);
            }

            foreach (var task in tasks)
            {
                if (task.AssignedAtUtc.HasValue)
                {
                    var day = task.AssignedAtUtc.Value.Date;
                    if (dayByDate.TryGetValue(day, out var bucket))
                        bucket.TasksAssigned++;
                }

                if (task.CompletedAtUtc.HasValue)
                {
                    var day = task.CompletedAtUtc.Value.Date;
                    if (dayByDate.TryGetValue(day, out var bucket))
                    {
                        bucket.TasksCompleted++;
                        if (task.Deadline.HasValue && task.CompletedAtUtc.Value > task.Deadline.Value)
                            bucket.OverdueCompleted++;
                    }
                }
            }

            var recentCompleted = tasks
                .Where(t => t.CompletedAtUtc.HasValue)
                .OrderByDescending(t => t.CompletedAtUtc!.Value)
                .Take(15)
                .Select(t =>
                {
                    var actualTicks = t.TimeEntries
                        .Where(e => e.EndTime.HasValue)
                        .Sum(e => e.Duration.Ticks);
                    var actualHours = Math.Round(TimeSpan.FromTicks(actualTicks).TotalHours, 2);

                    return new UserAnalyticsTaskDto
                    {
                        TaskId = t.Id,
                        Title = t.Title,
                        DeadlineUtc = t.Deadline,
                        CompletedAtUtc = t.CompletedAtUtc!.Value,
                        WasOverdue = t.Deadline.HasValue && t.CompletedAtUtc!.Value > t.Deadline.Value,
                        EstimatedHours = Math.Round(t.EstimatedHours, 2),
                        ActualHours = actualHours
                    };
                })
                .ToList();

            return Ok(new UserAnalyticsDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TasksAssigned = assigned,
                TasksCompleted = completed,
                TasksCompletedFromAssignedInPeriod = completedFromAssignedInPeriod,
                OverdueCompleted = overdueCompleted,
                WorkedHours = totalWorkedHours,
                Days = daysList,
                RecentCompletedTasks = recentCompleted
            });
        }
    }
}
