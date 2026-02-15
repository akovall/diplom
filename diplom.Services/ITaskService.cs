using diplom.Models;
using diplom.Models.enums;

namespace diplom.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetAllTasksAsync();
        Task<List<TaskItem>> GetTasksByProjectAsync(int projectId);
        Task<List<TaskItem>> GetTasksByStatusAsync(AppTaskStatus status);
        Task<TaskItem?> GetTaskByIdAsync(int id);
        Task<TaskItem> CreateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskAsync(TaskItem task);
        Task<bool> DeleteTaskAsync(int id);
        Task<TimeSpan> GetTotalTimeSpentAsync(int taskId);

        // Status mapping helpers
        string MapStatusToString(AppTaskStatus status);
        AppTaskStatus MapStringToStatus(string status);
        string FormatTimeSpan(TimeSpan ts);
    }
}
