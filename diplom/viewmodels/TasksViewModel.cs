using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Models.enums;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace diplom.viewmodels
{
    public partial class TasksViewModel : ObservableObject
    {
        // Event to request opening dialog from View
        public event EventHandler RequestOpenDialog;
        public event EventHandler<TaskDisplayItem> RequestEditTask;

        public ObservableCollection<TaskDisplayItem> Tasks { get; set; } = new();
        public ObservableCollection<Project> AvailableProjects { get; set; } = new();
        public ObservableCollection<User> AvailableAssignees { get; set; } = new();

        [ObservableProperty]
        private User? _selectedAssignee;

        // Employee can only assign to themselves
        public bool CanChangeAssignee => ApiClient.Instance.Role is "Admin" or "Manager";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = "All";

        [ObservableProperty]
        private string _selectedSortOption = "Priority";

        public string[] SortOptions { get; } = new[] { "Priority", "Status", "Deadline", "Title" };

        // === Create Task Dialog Properties ===
        [ObservableProperty]
        private bool _isCreateDialogOpen;

        [ObservableProperty]
        private string _newTaskTitle = string.Empty;

        [ObservableProperty]
        private string _newTaskDescription = string.Empty;

        [ObservableProperty]
        private TaskPriority _newTaskPriority = TaskPriority.Medium;

        [ObservableProperty]
        private Project _selectedProject;

        [ObservableProperty]
        private DateTime? _newTaskDeadline;

        [ObservableProperty]
        private double _newTaskEstimatedHours;

        [ObservableProperty]
        private bool _isLoading;

        // === Commands (auto-generated via [RelayCommand]) ===
        public IRelayCommand CreateTaskCommand { get; }
        public IRelayCommand<TaskDisplayItem> DeleteTaskCommand { get; }

        private ObservableCollection<TaskDisplayItem> _allTasks = new();
        private readonly AppDataService _dataService;
        private readonly ITaskService _taskService;
        private readonly IDialogService _dialogService;

        public TasksViewModel()
            : this(
                new ApiTaskService(),
                new DialogService())
        {
        }

        public TasksViewModel(ITaskService taskService, IDialogService dialogService)
        {
            _dataService = AppDataService.Instance;
            _taskService = taskService;
            _dialogService = dialogService;

            CreateTaskCommand = new RelayCommand(OpenCreateDialog);
            DeleteTaskCommand = new AsyncRelayCommand<TaskDisplayItem>(DeleteTaskAsync);

            LoadProjectsFromCache();
            LoadTasksFromCache();
            LoadAssigneesFromCache();
        }

        // === Partial methods for properties with side effects ===
        partial void OnSearchQueryChanged(string value) => FilterTasks();
        partial void OnSelectedStatusFilterChanged(string value) => FilterTasks();
        partial void OnSelectedSortOptionChanged(string value) => FilterTasks();

        private void LoadProjectsFromCache()
        {
            AvailableProjects.Clear();
            foreach (var project in _dataService.Projects)
            {
                AvailableProjects.Add(project);
            }

            if (AvailableProjects.Any() && SelectedProject == null)
            {
                SelectedProject = AvailableProjects.First();
            }
        }

        private void LoadAssigneesFromCache()
        {
            AvailableAssignees.Clear();
            if (CanChangeAssignee)
            {
                // Manager/Admin: show all assignable users
                foreach (var user in _dataService.Users)
                    AvailableAssignees.Add(user);
            }
            else
            {
                // Employee: only themselves
                var self = new User
                {
                    Id = ApiClient.Instance.UserId,
                    FullName = ApiClient.Instance.FullName
                };
                AvailableAssignees.Add(self);
            }
            SelectedAssignee = AvailableAssignees.FirstOrDefault();
        }

        private void LoadTasksFromCache()
        {
            _allTasks.Clear();
            Tasks.Clear();

            foreach (var task in _dataService.Tasks)
            {
                var timeSpent = task.TimeEntries != null && task.TimeEntries.Any()
                    ? TimeSpan.FromTicks(task.TimeEntries.Sum(e => e.Duration.Ticks))
                    : TimeSpan.Zero;

                var displayItem = MapToDisplayItem(task, timeSpent);
                _allTasks.Add(displayItem);
                Tasks.Add(displayItem);
            }
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            IsLoading = true;
            try
            {
                await _dataService.RefreshTasksAsync();
                await _dataService.RefreshProjectsAsync();
                LoadProjectsFromCache();
                LoadTasksFromCache();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private TaskDisplayItem MapToDisplayItem(TaskItem task, TimeSpan timeSpent)
        {
            var item = new TaskDisplayItem
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = (int)task.Priority,
                Status = _taskService.MapStatusToString(task.Status),
                TimeSpentFormatted = _taskService.FormatTimeSpan(timeSpent),
                ProjectName = task.Project?.Title ?? "No Project",
                ProjectId = task.ProjectId,
                Deadline = task.Deadline,
                IsActive = false,
                OnStatusChanged = OnTaskStatusChanged
            };

            item.ToggleTimerCommand = new RelayCommand(() => item.IsActive = !item.IsActive);
            item.EditCommand = new RelayCommand(() => RequestEditTask?.Invoke(this, item));
            item.DeleteCommand = new AsyncRelayCommand(async () => await DeleteTaskAsync(item));

            return item;
        }

        private void OnTaskStatusChanged(TaskDisplayItem displayItem) => _ = SaveStatusAsync(displayItem);

        public async Task SaveStatusAsync(TaskDisplayItem displayItem)
        {
            try
            {
                var task = _dataService.Tasks.FirstOrDefault(t => t.Id == displayItem.Id);
                if (task != null)
                {
                    task.Status = _taskService.MapStringToStatus(displayItem.Status);
                    await _dataService.UpdateTaskAsync(task);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error saving status: {ex.Message}");
            }
        }

        public async Task SaveTaskEditAsync(TaskDisplayItem displayItem)
        {
            try
            {
                var dbTask = _dataService.Tasks.Find(t => t.Id == displayItem.Id);
                if (dbTask != null)
                {
                    dbTask.Title = displayItem.Title;
                    dbTask.Description = displayItem.Description;
                    dbTask.Status = _taskService.MapStringToStatus(displayItem.Status);
                    dbTask.Priority = (TaskPriority)displayItem.Priority;
                    dbTask.Deadline = displayItem.Deadline;

                    await _dataService.UpdateTaskAsync(dbTask);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error saving task: {ex.Message}");
            }
        }

        private void OpenCreateDialog()
        {
            ResetCreateForm();
            IsCreateDialogOpen = true;
            RequestOpenDialog?.Invoke(this, EventArgs.Empty);
        }

        public void ResetCreateForm()
        {
            NewTaskTitle = string.Empty;
            NewTaskDescription = string.Empty;
            NewTaskPriority = TaskPriority.Medium;
            NewTaskDeadline = null;
            NewTaskEstimatedHours = 0;
            if (AvailableProjects.Any())
                SelectedProject = AvailableProjects.First();
            SelectedAssignee = AvailableAssignees.FirstOrDefault();
        }

        public void ExecuteCreateTask() => _ = CreateTaskAsync();

        private async Task CreateTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle))
            {
                _dialogService.ShowWarning("Task title is required", "Validation");
                return;
            }

            if (SelectedProject == null)
            {
                _dialogService.ShowWarning("Please select a project", "Validation");
                return;
            }

            try
            {
                var newTask = new TaskItem
                {
                    Title = NewTaskTitle.Trim(),
                    Description = NewTaskDescription?.Trim() ?? string.Empty,
                    Priority = NewTaskPriority,
                    Status = AppTaskStatus.ToDo,
                    ProjectId = SelectedProject.Id,
                    Deadline = NewTaskDeadline,
                    EstimatedHours = NewTaskEstimatedHours,
                    AssigneeId = SelectedAssignee?.Id
                };

                var created = await _dataService.CreateTaskAsync(newTask);
                var displayItem = MapToDisplayItem(created, TimeSpan.Zero);

                _allTasks.Insert(0, displayItem);
                Tasks.Insert(0, displayItem);

                IsCreateDialogOpen = false;
                ResetCreateForm();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error creating task: {ex.Message}");
            }
        }

        private async Task DeleteTaskAsync(TaskDisplayItem task)
        {
            if (task == null) return;

            if (!_dialogService.Confirm($"Are you sure you want to delete '{task.Title}'?", "Confirm Delete"))
                return;

            try
            {
                var success = await _dataService.DeleteTaskAsync(task.Id);
                if (success)
                {
                    _allTasks.Remove(task);
                    Tasks.Remove(task);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error deleting task: {ex.Message}");
            }
        }

        private void FilterTasks()
        {
            Tasks.Clear();

            var filtered = _allTasks.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "All")
                filtered = filtered.Where(t => t.Status == SelectedStatusFilter);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.ToLower();
                filtered = filtered.Where(t =>
                    t.Title.ToLower().Contains(query) ||
                    t.Description.ToLower().Contains(query) ||
                    t.ProjectName.ToLower().Contains(query));
            }

            var sorted = SelectedSortOption switch
            {
                "Status" => filtered.OrderBy(t => GetStatusOrder(t.Status)).ThenByDescending(t => t.Priority),
                "Deadline" => filtered.OrderBy(t => t.Deadline ?? DateTime.MaxValue).ThenByDescending(t => t.Priority),
                "Title" => filtered.OrderBy(t => t.Title).ThenByDescending(t => t.Priority),
                _ => filtered.OrderByDescending(t => t.Priority)
            };

            foreach (var task in sorted)
            {
                Tasks.Add(task);
            }
        }

        private static int GetStatusOrder(string status) => status switch
        {
            "In Progress" => 0,
            "To Do" => 1,
            "On Hold" => 2,
            "Done" => 3,
            _ => 4
        };
    }
}