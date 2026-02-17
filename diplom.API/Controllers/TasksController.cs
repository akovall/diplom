using diplom.Data;
using diplom.Models;
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
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
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
            task.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            var created = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, created);
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskItem>> Update(int id, [FromBody] TaskItem task)
        {
            var existing = await _context.Tasks.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Status = task.Status;
            existing.Priority = task.Priority;
            existing.Deadline = task.Deadline;
            existing.EstimatedHours = task.EstimatedHours;
            existing.ProjectId = task.ProjectId;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
