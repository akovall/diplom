using diplom.Data;
using diplom.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var today = DateTime.Today;

            // Admin/Manager see all tasks, Employee sees only assigned
            var tasksQuery = _context.Tasks.AsQueryable();
            if (role == "Employee")
                tasksQuery = tasksQuery.Where(t => t.AssigneeId == userId);

            var tasks = await tasksQuery.ToListAsync();

            // Time logs: always personal
            var todayLogs = await _context.TimeLogs
                .Where(e => e.UserId == userId)
                .Where(e => e.StartTime.Date == today || (e.EndTime.HasValue && e.EndTime.Value.Date == today))
                .ToListAsync();

            var workedTicks = todayLogs.Sum(e => e.Duration.Ticks);
            var workedTime = TimeSpan.FromTicks(workedTicks);

            var totalTasks = tasks.Count;
            var doneTasks = tasks.Count(t => t.Status == AppTaskStatus.Done);
            var productivity = totalTasks == 0 ? 0 : Math.Round((double)doneTasks / totalTasks * 100, 1);

            return Ok(new
            {
                workedTodayFormatted = $"{(int)workedTime.TotalHours:D2}:{workedTime.Minutes:D2}",
                tasksInProgress = tasks.Count(t => t.Status == AppTaskStatus.InProgress),
                urgentTasks = tasks.Count(t => t.Priority == TaskPriority.Critical && t.Status != AppTaskStatus.Done),
                productivity,
                productivityText = $"{productivity}%"
            });
        }
    }
}
