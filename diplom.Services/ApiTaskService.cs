using diplom.Models;
using diplom.Models.enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace diplom.Services
{
    public class ApiTaskService : ITaskService
    {
        private readonly ApiClient _api;

        public ApiTaskService()
        {
            _api = ApiClient.Instance;
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await _api.GetAsync<List<TaskItem>>("/api/tasks") ?? new();
        }

        public async Task<List<TaskItem>> GetTasksByProjectAsync(int projectId)
        {
            return await _api.GetAsync<List<TaskItem>>($"/api/tasks/project/{projectId}") ?? new();
        }

        public async Task<List<TaskItem>> GetTasksByStatusAsync(AppTaskStatus status)
        {
            // Filter client-side since API doesn't have a status filter endpoint
            var all = await GetAllTasksAsync();
            return all.Where(t => t.Status == status).ToList();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await _api.GetAsync<TaskItem>($"/api/tasks/{id}");
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            return await _api.PostAsync<TaskItem>("/api/tasks", task) ?? task;
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            return await _api.PutAsync<TaskItem>($"/api/tasks/{task.Id}", task) ?? task;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            try
            {
                await _api.DeleteAsync($"/api/tasks/{id}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TimeSpan> GetTotalTimeSpentAsync(int taskId)
        {
            var entries = await _api.GetAsync<List<TimeEntry>>($"/api/timeentries/task/{taskId}");
            if (entries == null || entries.Count == 0)
                return TimeSpan.Zero;

            var totalTicks = entries.Sum(e => e.Duration.Ticks);
            return TimeSpan.FromTicks(totalTicks);
        }

        // Status mapping helpers (same as before — pure logic, no DB needed)
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
