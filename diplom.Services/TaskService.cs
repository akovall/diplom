using diplom.Data;
using diplom.Models;
using diplom.Models.enums;
using Microsoft.EntityFrameworkCore;

namespace diplom.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.TimeEntries)
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksByProjectAsync(int projectId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.TimeEntries)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.Priority)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksByStatusAsync(AppTaskStatus status)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TimeEntries)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.Priority)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            task.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            
            // Reload with related entities
            return await GetTaskByIdAsync(task.Id) ?? task;
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            var existing = await _context.Tasks.FindAsync(task.Id);
            if (existing == null)
                throw new InvalidOperationException($"Task with ID {task.Id} not found");

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Status = task.Status;
            existing.Priority = task.Priority;
            existing.Deadline = task.Deadline;
            existing.EstimatedHours = task.EstimatedHours;
            existing.ProjectId = task.ProjectId;
            existing.AssigneeId = task.AssigneeId;

            await _context.SaveChangesAsync();
            return await GetTaskByIdAsync(task.Id) ?? existing;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TimeSpan> GetTotalTimeSpentAsync(int taskId)
        {
            var entries = await _context.TimeLogs
                .Where(t => t.TaskId == taskId)
                .ToListAsync();

            var totalTicks = entries.Sum(e => e.Duration.Ticks);
            return TimeSpan.FromTicks(totalTicks);
        }
        // Status mapping helpers
        public string MapStatusToString(AppTaskStatus status)
        {
            return status switch
            {
                AppTaskStatus.ToDo => "To Do",
                AppTaskStatus.InProgress => "In Progress",
                AppTaskStatus.OnHold => "On Hold",
                AppTaskStatus.Done => "Done",
                _ => "To Do"
            };
        }

        public AppTaskStatus MapStringToStatus(string status)
        {
            return status switch
            {
                "To Do" => AppTaskStatus.ToDo,
                "In Progress" => AppTaskStatus.InProgress,
                "On Hold" => AppTaskStatus.OnHold,
                "Done" => AppTaskStatus.Done,
                _ => AppTaskStatus.ToDo
            };
        }

        public string FormatTimeSpan(TimeSpan ts)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }
    }
}
