using diplom.Data;
using diplom.Models;
using Microsoft.EntityFrameworkCore;

namespace diplom.Services
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        public async Task<List<Project>> GetActiveProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => !p.IsArchived)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            project.CreatedAt = DateTime.UtcNow;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            var existing = await _context.Projects.FindAsync(project.Id);
            if (existing == null)
                throw new InvalidOperationException($"Project with ID {project.Id} not found");

            existing.Title = project.Title;
            existing.Description = project.Description;
            existing.IsArchived = project.IsArchived;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return false;

            project.IsArchived = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
