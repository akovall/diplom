using diplom.Data;
using diplom.Models.Analytics;
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

        private static DateTime GetWeekStartLocal(DateTime localDate)
        {
            var date = localDate.Date;
            var dayOfWeek = (int)date.DayOfWeek; // Sunday=0
            var mondayBased = dayOfWeek == 0 ? 7 : dayOfWeek; // Monday=1..Sunday=7
            return date.AddDays(-(mondayBased - 1));
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var today = DateTime.Today;
            var nowLocal = DateTime.Now;

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

            // Smart productivity: week-specific, smoothed (1 done out of 1 created this week != 100%).
            var weekStartLocal = GetWeekStartLocal(nowLocal);
            var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartLocal);
            var weekEndUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartLocal.AddDays(7));
            var smart = ProductivityCalculator.CalculateForWeek(tasks, weekStartUtc, weekEndUtc);
            var productivity = smart.ScorePercent;

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
