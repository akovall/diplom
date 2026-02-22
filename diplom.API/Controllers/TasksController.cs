using diplom.Data;
using diplom.API.Hubs;
using diplom.Models;
using diplom.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TimeHub> _hub;

        public TasksController(AppDbContext context, IHubContext<TimeHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? UserRole.Employee.ToString();
        }

        private bool IsAdminOrManager()
        {
            var role = GetCurrentUserRole();
            return role is "Admin" or "Manager";
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<List<TaskItem>>> GetAll()
        {
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.TimeEntries)
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetById(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        // GET: api/tasks/my — tasks assigned to current user
        [HttpGet("my")]
        public async Task<ActionResult<List<TaskItem>>> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TimeEntries)
                .Where(t => t.AssigneeId == userId)
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/project/2
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<TaskItem>>> GetByProject(int projectId)
        {
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TimeEntries)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.Priority)
                .ToListAsync();

            return Ok(tasks);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskItem>> Create([FromBody] TaskItem task)
        {
            var userId = GetCurrentUserId();
            var isAdminOrManager = IsAdminOrManager();

            if (!isAdminOrManager)
            {
                // Employees can create tasks only for themselves
                task.AssigneeId = userId;
            }

            task.CreatedAt = DateTime.UtcNow;
            if (task.AssigneeId.HasValue)
                task.AssignedAtUtc = DateTime.UtcNow;
            task.CompletedAtUtc = task.Status == AppTaskStatus.Done ? DateTime.UtcNow : null;
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            var created = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            await _hub.Clients.All.SendAsync("TaskChanged", task.Id);

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, created);
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskItem>> Update(int id, [FromBody] TaskItem task)
        {
            var userId = GetCurrentUserId();
            var isAdminOrManager = IsAdminOrManager();

            var existing = await _context.Tasks.FindAsync(id);
            if (existing == null)
                return NotFound();

            if (!isAdminOrManager)
            {
                // Employees can edit only tasks assigned to themselves
                if (existing.AssigneeId != userId)
                    return Forbid();
            }

            existing.Title = task.Title;
            existing.Description = task.Description;

            if (existing.Status != task.Status)
            {
                // Keep CompletedAtUtc in sync with Done transitions.
                if (task.Status == AppTaskStatus.Done)
                    existing.CompletedAtUtc = DateTime.UtcNow;
                else if (existing.Status == AppTaskStatus.Done)
                    existing.CompletedAtUtc = null;

                existing.Status = task.Status;
            }
            existing.Priority = task.Priority;
            existing.Deadline = task.Deadline;
            existing.EstimatedHours = task.EstimatedHours;

            // Only Manager/Admin can reassign or move between projects
            if (isAdminOrManager)
            {
                existing.ProjectId = task.ProjectId;
                if (existing.AssigneeId != task.AssigneeId)
                {
                    existing.AssigneeId = task.AssigneeId;
                    existing.AssignedAtUtc = task.AssigneeId.HasValue ? DateTime.UtcNow : null;
                }
            }

            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("TaskChanged", existing.Id);
            return Ok(existing);
        }

        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var isAdminOrManager = IsAdminOrManager();

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            if (!isAdminOrManager)
            {
                if (task.AssigneeId != userId)
                    return Forbid();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("TaskChanged", task.Id);
            return NoContent();
        }
    }
}
