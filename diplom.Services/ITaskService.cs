using diplom.Models;

namespace diplom.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetAllTasksAsync();
        Task<List<TaskItem>> GetTasksByProjectAsync(int projectId);
        Task<List<TaskItem>> GetTasksByStatusAsync(Models.enums.AppTaskStatus status);
        Task<TaskItem?> GetTaskByIdAsync(int id);
        Task<TaskItem> CreateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskAsync(TaskItem task);
        Task<bool> DeleteTaskAsync(int id);
        Task<TimeSpan> GetTotalTimeSpentAsync(int taskId);
    }
}
