using diplom.Data;
using diplom.Models;
using diplom.Models.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/projects
        [HttpGet]
        public async Task<ActionResult<List<Project>>> GetAll()
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        // GET: api/projects/active
        [HttpGet("active")]
        public async Task<ActionResult<List<Project>>> GetActive()
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => !p.IsArchived)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        // GET: api/projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetById(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            return Ok(project);
        }

        // GET: api/projects/5/analytics?days=30
        [HttpGet("{id}/analytics")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ProjectAnalyticsDto>> GetProjectAnalytics(
            int id,
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            if (days < 7) days = 7;
            if (days > 90) days = 90;

            var project = await _context.Projects
                .Where(p => p.Id == id)
                .Select(p => new { p.Id, p.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (project == null)
                return NotFound();

            var toUtc = DateTime.UtcNow;
            var fromUtc = toUtc.Date.AddDays(-days + 1);

            var tasks = await _context.Tasks
                .Include(t => t.TimeEntries)
                .Include(t => t.Assignee)
                .Where(t => t.ProjectId == id)
                .Where(t =>
                    (t.AssignedAtUtc.HasValue && t.AssignedAtUtc.Value >= fromUtc && t.AssignedAtUtc.Value <= toUtc) ||
                    (t.CompletedAtUtc.HasValue && t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc))
                .ToListAsync(cancellationToken);

            var logs = await _context.TimeLogs
                .Include(l => l.Task)
                .Where(l => l.Task != null && l.Task.ProjectId == id)
                .Where(l => l.EndTime.HasValue)
                .Where(l => l.EndTime!.Value >= fromUtc && l.EndTime!.Value <= toUtc)
                .ToListAsync(cancellationToken);

            var totalWorkedHours = Math.Round(TimeSpan.FromTicks(logs.Sum(l => l.Duration.Ticks)).TotalHours, 2);

            var assigned = tasks.Count(t => t.AssignedAtUtc.HasValue && t.AssignedAtUtc.Value >= fromUtc && t.AssignedAtUtc.Value <= toUtc);
            var completed = tasks.Count(t => t.CompletedAtUtc.HasValue && t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc);
            var overdueCompleted = tasks.Count(t =>
                t.CompletedAtUtc.HasValue &&
                t.CompletedAtUtc.Value >= fromUtc && t.CompletedAtUtc.Value <= toUtc &&
                t.Deadline.HasValue &&
                t.CompletedAtUtc.Value > t.Deadline.Value);

            var daysList = Enumerable.Range(0, days)
                .Select(i => fromUtc.Date.AddDays(i))
                .Select(day => new ProjectAnalyticsDayDto
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
                .Take(20)
                .Select(t =>
                {
                    var actualTicks = t.TimeEntries
                        .Where(e => e.EndTime.HasValue)
                        .Sum(e => e.Duration.Ticks);
                    var actualHours = Math.Round(TimeSpan.FromTicks(actualTicks).TotalHours, 2);

                    return new ProjectAnalyticsTaskDto
                    {
                        TaskId = t.Id,
                        Title = t.Title,
                        DeadlineUtc = t.Deadline,
                        CompletedAtUtc = t.CompletedAtUtc!.Value,
                        WasOverdue = t.Deadline.HasValue && t.CompletedAtUtc!.Value > t.Deadline.Value,
                        EstimatedHours = Math.Round(t.EstimatedHours, 2),
                        ActualHours = actualHours,
                        AssigneeName = t.Assignee?.FullName ?? string.Empty
                    };
                })
                .ToList();

            return Ok(new ProjectAnalyticsDto
            {
                ProjectId = project.Id,
                ProjectTitle = project.Title,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TasksAssigned = assigned,
                TasksCompleted = completed,
                OverdueCompleted = overdueCompleted,
                WorkedHours = totalWorkedHours,
                Days = daysList,
                RecentCompletedTasks = recentCompleted
            });
        }

        // POST: api/projects
        [HttpPost]
        public async Task<ActionResult<Project>> Create([FromBody] Project project)
        {
            project.CreatedAt = DateTime.UtcNow;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }

        // PUT: api/projects/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Project>> Update(int id, [FromBody] Project project)
        {
            var existing = await _context.Projects.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Title = project.Title;
            existing.Description = project.Description;
            existing.IsArchived = project.IsArchived;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE: api/projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/projects/5/archive
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> Archive(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            project.IsArchived = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
