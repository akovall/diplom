using diplom.Data;
using diplom.Models;
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
