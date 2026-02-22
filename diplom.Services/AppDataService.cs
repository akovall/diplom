using diplom.Models;
using diplom.Models.Analytics;
using diplom.Models.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace diplom.Services
{
    /// <summary>
    /// Singleton service that caches application data loaded from the API
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

        private readonly ApiClient _api;
        
        // Cached data
        public List<TaskItem> Tasks { get; private set; } = new();
        public List<Project> Projects { get; private set; } = new();
        public List<TimeEntry> TimeEntries { get; private set; } = new();
        public List<User> Users { get; private set; } = new();
        
        // Loading state
        public bool IsLoaded { get; private set; } = false;
        public bool IsLoading { get; private set; } = false;
        public string LoadingStatus { get; private set; } = "";
        
        // Events
        public event Action? DataLoaded;
        public event Action<string>? LoadingStatusChanged;

        private AppDataService()
        {
            _api = ApiClient.Instance;
        }

        public async Task LoadAllDataAsync()
        {
            if (IsLoading) return;
            
            IsLoading = true;
            
            try
            {
                // Load Projects from API
                UpdateStatus("Loading projects...");
                Projects = await _api.GetAsync<List<Project>>("/api/projects") ?? new();

                // Load Tasks from API
                UpdateStatus("Loading tasks...");
                Tasks = await _api.GetAsync<List<TaskItem>>("/api/tasks") ?? new();

                // Load time entries from API
                UpdateStatus("Loading time entries...");
                TimeEntries = await _api.GetAsync<List<TimeEntry>>("/api/timeentries/today") ?? new();
                AttachTaskReferencesToTimeEntries();

                // Load assignable users (only for Manager/Admin)
                if (_api.Role is "Admin" or "Manager")
                {
                    UpdateStatus("Loading users...");
                    Users = await _api.GetAsync<List<User>>("/api/users/assignable") ?? new();
                }

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
            Tasks = await _api.GetAsync<List<TaskItem>>("/api/tasks") ?? new();
            AttachTaskReferencesToTimeEntries();
            DataLoaded?.Invoke();
        }

        public async Task RefreshTimeEntriesTodayAsync()
        {
            TimeEntries = await _api.GetAsync<List<TimeEntry>>("/api/timeentries/today") ?? new();
            AttachTaskReferencesToTimeEntries();
            DataLoaded?.Invoke();
        }

        public async Task RefreshProjectsAsync()
        {
            Projects = await _api.GetAsync<List<Project>>("/api/projects") ?? new();
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

        public int GetTasksDoneCount()
        {
            return Tasks.Count(t => t.Status == AppTaskStatus.Done);
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

        public List<TaskItem> GetTopUrgentTasks(int count = 3)
        {
            return Tasks
                .Where(t => t.Status != AppTaskStatus.Done)
                .Where(t => t.Deadline.HasValue)
                .OrderBy(t => t.Deadline)
                .ThenByDescending(t => t.Priority)
                .Take(count)
                .ToList();
        }

        public double GetProductivityPercentage()
        {
            var totalTasks = Tasks.Count;
            if (totalTasks == 0) return 0;
            
            var doneTasks = Tasks.Count(t => t.Status == AppTaskStatus.Done);
            return Math.Round((double)doneTasks / totalTasks * 100, 1);
        }

        public ProductivityResult GetSmartProductivityForWeekLocal(DateTime weekStartLocal)
        {
            var weekStart = weekStartLocal.Date;
            var weekEnd = weekStart.AddDays(7);

            // Tasks/entries are stored as UTC in DB; compare using UTC window.
            var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStart);
            var weekEndUtc = TimeZoneInfo.ConvertTimeToUtc(weekEnd);

            return ProductivityCalculator.CalculateForWeek(Tasks, weekStartUtc, weekEndUtc);
        }

        public static DateTime GetWeekStartLocal(DateTime localDate)
        {
            // ISO-ish week: Monday start, Sunday end.
            var date = localDate.Date;
            var dayOfWeek = (int)date.DayOfWeek; // Sunday=0
            var mondayBased = dayOfWeek == 0 ? 7 : dayOfWeek; // Monday=1..Sunday=7
            return date.AddDays(-(mondayBased - 1));
        }

        public List<TaskItem> GetRecentTasks(int count = 5)
        {
            return Tasks
                .Where(t => t.Status != AppTaskStatus.Done)
                .Take(count)
                .ToList();
        }

        // CRUD operations via API (update cache after)
        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            var created = await _api.PostAsync<TaskItem>("/api/tasks", task);
            
            if (created != null)
            {
                Tasks.Insert(0, created);
            }
            
            return created ?? task;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            try
            {
                await _api.DeleteAsync($"/api/tasks/{id}");
                Tasks.RemoveAll(t => t.Id == id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            var updated = await _api.PutAsync<TaskItem>($"/api/tasks/{task.Id}", task);
            
            // Update cache
            var index = Tasks.FindIndex(t => t.Id == task.Id);
            if (index >= 0 && updated != null)
            {
                Tasks[index] = updated;
            }
            
            return updated ?? task;
        }

        public async Task<TimeEntry> CreateTimeEntryAsync(TimeEntry entry)
        {
            var created = await _api.PostAsync<TimeEntry>("/api/timeentries", entry);
            if (created != null)
            {
                var task = Tasks.FirstOrDefault(t => t.Id == created.TaskId);
                if (task != null)
                    created.Task = task;

                TimeEntries.Insert(0, created);
                if (task != null)
                {
                    task.TimeEntries ??= new List<TimeEntry>();
                    task.TimeEntries.Add(created);
                }
            }
            return created ?? entry;
        }

        private void AttachTaskReferencesToTimeEntries()
        {
            if (Tasks.Count == 0 || TimeEntries.Count == 0)
                return;

            var taskById = Tasks.ToDictionary(t => t.Id, t => t);
            foreach (var entry in TimeEntries)
            {
                if (entry.Task == null && taskById.TryGetValue(entry.TaskId, out var task))
                    entry.Task = task;
            }
        }
    }
}
