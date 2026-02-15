using diplom.Data;
using diplom.Models;
using diplom.Models.enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace diplom.Services
{
    /// <summary>
    /// Singleton service that caches application data at startup
    /// </summary>
    public class AppDataService
    {
        private static AppDataService? _instance;
        private static readonly object _lock = new();
        
        public static AppDataService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AppDataService();
                    }
                }
                return _instance;
            }
        }

        private readonly AppDbContext _context;
        
        // Cached data
        public List<TaskItem> Tasks { get; private set; } = new();
        public List<Project> Projects { get; private set; } = new();
        public List<TimeEntry> TimeEntries { get; private set; } = new();
        
        // Loading state
        public bool IsLoaded { get; private set; } = false;
        public bool IsLoading { get; private set; } = false;
        public string LoadingStatus { get; private set; } = "";
        
        // Events
        public event Action? DataLoaded;
        public event Action<string>? LoadingStatusChanged;

        private AppDataService()
        {
            _context = new AppDbContext();
        }

        public async Task LoadAllDataAsync()
        {
            if (IsLoading) return;
            
            IsLoading = true;
            
            try
            {
                // Load Projects
                UpdateStatus("Loading projects...");
                Projects = await _context.Projects
                    .Where(p => !p.IsArchived)
                    .OrderBy(p => p.Title)
                    .ToListAsync();

                // Load Tasks with related data
                UpdateStatus("Loading tasks...");
                Tasks = await _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.Assignee)
                    .Include(t => t.TimeEntries)
                    .OrderByDescending(t => t.Priority)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();

                // Load today's time entries
                UpdateStatus("Loading time entries...");
                var today = DateTime.Today;
                TimeEntries = await _context.TimeLogs
                    .Include(t => t.Task)
                    .Where(t => t.StartTime.Date == today || (t.EndTime.HasValue && t.EndTime.Value.Date == today))
                    .OrderByDescending(t => t.StartTime)
                    .ToListAsync();

                IsLoaded = true;
                UpdateStatus("Ready!");
                DataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateStatus(string status)
        {
            LoadingStatus = status;
            LoadingStatusChanged?.Invoke(status);
        }

        // Refresh individual collections
        public async Task RefreshTasksAsync()
        {
            Tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.TimeEntries)
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
            
            DataLoaded?.Invoke();
        }

        public async Task RefreshProjectsAsync()
        {
            Projects = await _context.Projects
                .Where(p => !p.IsArchived)
                .OrderBy(p => p.Title)
                .ToListAsync();
            
            DataLoaded?.Invoke();
        }

        // Helper methods for dashboard statistics
        public TimeSpan GetTodayWorkedTime()
        {
            return TimeSpan.FromTicks(TimeEntries.Sum(e => e.Duration.Ticks));
        }

        public int GetTasksInProgressCount()
        {
            return Tasks.Count(t => t.Status == AppTaskStatus.InProgress);
        }

        public int GetTasksDoneToday()
        {
            var today = DateTime.Today;
            return Tasks.Count(t => t.Status == AppTaskStatus.Done && 
                                   t.CreatedAt.Date == today);
        }

        public int GetUrgentTasksCount()
        {
            return Tasks.Count(t => t.Priority == TaskPriority.Critical && 
                                   t.Status != AppTaskStatus.Done);
        }

        public double GetProductivityPercentage()
        {
            var totalTasks = Tasks.Count;
            if (totalTasks == 0) return 0;
            
            var doneTasks = Tasks.Count(t => t.Status == AppTaskStatus.Done);
            return Math.Round((double)doneTasks / totalTasks * 100, 1);
        }

        public List<TaskItem> GetRecentTasks(int count = 5)
        {
            return Tasks
                .Where(t => t.Status != AppTaskStatus.Done)
                .Take(count)
                .ToList();
        }

        // CRUD operations that update cache
        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            task.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            
            // Reload with related entities
            var created = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == task.Id);
            
            if (created != null)
            {
                Tasks.Insert(0, created);
            }
            
            return created ?? task;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            
            Tasks.RemoveAll(t => t.Id == id);
            return true;
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

            await _context.SaveChangesAsync();
            
            // Update cache
            var index = Tasks.FindIndex(t => t.Id == task.Id);
            if (index >= 0)
            {
                var updated = await _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.TimeEntries)
                    .FirstOrDefaultAsync(t => t.Id == task.Id);
                
                if (updated != null)
                {
                    Tasks[index] = updated;
                }
            }
            
            return existing;
        }
    }
}
