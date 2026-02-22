using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Models.enums;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace diplom.viewmodels
{
    public partial class TasksViewModel : ObservableObject
    {
        // Event to request opening dialog from View
        public event EventHandler RequestOpenDialog;
        public event EventHandler<TaskDisplayItem> RequestEditTask;
        public event EventHandler<TaskDisplayItem> RequestViewTaskDetails;

        public ObservableCollection<TaskDisplayItem> Tasks { get; set; } = new();
        public ObservableCollection<Project> AvailableProjects { get; set; } = new();
        public ObservableCollection<User> AvailableAssignees { get; set; } = new();

        [ObservableProperty]
        private User _selectedAssignee;

        // Employee can only assign to themselves
        public bool CanChangeAssignee => ApiClient.Instance.Role is "Admin" or "Manager";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = "All";

        [ObservableProperty]
        private string _selectedSortOption = "Priority";

        public string[] SortOptions { get; } = new[] { "Priority", "Status", "Deadline", "Title", "My Tasks" };

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
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly IDialogService _dialogService;
        private DispatcherTimer _activeTimer;
        private bool _suppressStatusSave;

        public TasksViewModel()
            : this(
                new ApiTaskService(),
                new DialogService(),
                TimeTrackingService.Instance)
        {
        }

        public TasksViewModel(ITaskService taskService, IDialogService dialogService, ITimeTrackingService timeTrackingService)
        {
            _dataService = AppDataService.Instance;
            _taskService = taskService;
            _dialogService = dialogService;
            _timeTrackingService = timeTrackingService;

            CreateTaskCommand = new RelayCommand(OpenCreateDialog);
            DeleteTaskCommand = new AsyncRelayCommand<TaskDisplayItem>(DeleteTaskAsync);

            // Intentionally disable live ticking on task cards; time is refreshed after stop/sync.
            _activeTimer = new DispatcherTimer();

            LoadProjectsFromCache();
            LoadTasksFromCache();
            LoadAssigneesFromCache();

            // Ensure tasks list reflects server changes made by other clients.
            _ = RefreshDataAsync();
        }

        private async Task ToggleTimerAsync(TaskDisplayItem item)
        {
            if (item == null) return;
            if (item.AssigneeId != ApiClient.Instance.UserId)
            {
                _dialogService.ShowWarning("You can track time only for tasks assigned to you.", "Access denied");
                return;
            }

            if (_timeTrackingService.HasActiveSession && _timeTrackingService.ActiveTaskId == item.Id)
            {
                try
                {
                    await _timeTrackingService.StopActiveAsync();
                    item.IsActive = false;
                    item.ActiveStartTime = null;
                    _activeTimer.Stop();

                    await _dataService.RefreshTasksAsync();
                    LoadTasksFromCache();
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError($"Failed to stop timer: {ex.Message}");
                }
            }
            else
            {
                if (_timeTrackingService.HasActiveSession && _timeTrackingService.ActiveTaskId.HasValue)
                {
                    var currentlyActive = _allTasks.FirstOrDefault(t => t.Id == _timeTrackingService.ActiveTaskId.Value);
                    if (currentlyActive != null)
                    {
                        try
                        {
                            await _timeTrackingService.StopActiveAsync();
                            currentlyActive.IsActive = false;
                            currentlyActive.ActiveStartTime = null;
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowError($"Failed to switch timer: {ex.Message}");
                            return;
                        }
                    }
                }

                _timeTrackingService.Start(item.Id);
                item.IsActive = true;
                item.ActiveStartTime = _timeTrackingService.ActiveStartTimeLocal;
                // no live tick
            }
        }

        private void SyncActiveTaskState()
        {
            foreach (var task in _allTasks)
            {
                task.IsActive = false;
                task.ActiveStartTime = null;
                task.TimeSpentFormatted = _timeTrackingService.FormatTimeSpan(task.AccumulatedTime);
            }

            if (_timeTrackingService.HasActiveSession && _timeTrackingService.ActiveTaskId.HasValue)
            {
                var activeTask = _allTasks.FirstOrDefault(t => t.Id == _timeTrackingService.ActiveTaskId.Value);
                if (activeTask != null)
                {
                    activeTask.IsActive = true;
                    activeTask.ActiveStartTime = _timeTrackingService.ActiveStartTimeLocal;
                    activeTask.TimeSpentFormatted = _timeTrackingService.FormatTimeSpan(activeTask.AccumulatedTime);
                    _activeTimer.Stop();
                    return;
                }
            }

            _activeTimer.Stop();
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
                // Ignore open entries (EndTime == null) to avoid UI double-counting while tracking.
                var timeSpent = task.TimeEntries != null && task.TimeEntries.Any()
                    ? TimeSpan.FromTicks(task.TimeEntries.Where(e => e.EndTime.HasValue).Sum(e => e.Duration.Ticks))
                    : TimeSpan.Zero;

                var displayItem = MapToDisplayItem(task, timeSpent);
                _allTasks.Add(displayItem);
                Tasks.Add(displayItem);
            }

            SyncActiveTaskState();
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
            var isAdminOrManager = ApiClient.Instance.Role is "Admin" or "Manager";
            var canModify = isAdminOrManager || (task.AssigneeId.HasValue && task.AssigneeId.Value == ApiClient.Instance.UserId);

            var item = new TaskDisplayItem
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = (int)task.Priority,
                Status = _taskService.MapStatusToString(task.Status),
                TimeSpentFormatted = _timeTrackingService.FormatTimeSpan(timeSpent),
                ProjectName = task.Project?.Title ?? "No Project",
                ProjectId = task.ProjectId,
                Deadline = task.Deadline,
                AssigneeId = task.AssigneeId,
                AssigneeName = task.Assignee?.FullName ?? string.Empty,
                CanEdit = canModify,
                CanDelete = canModify,
                CanViewDetails = true,
                IsActive = false,
                AccumulatedTime = timeSpent,
                OnStatusChanged = OnTaskStatusChanged
            };

            item.ToggleTimerCommand = new AsyncRelayCommand(() => ToggleTimerAsync(item));
            item.EditCommand = new RelayCommand(() =>
            {
                if (!item.CanEdit)
                {
                    _dialogService.ShowWarning("You can edit only tasks assigned to you.", "Access denied");
                    return;
                }
                RequestEditTask?.Invoke(this, item);
            });
            item.DetailsCommand = new RelayCommand(() => RequestViewTaskDetails?.Invoke(this, item));
            item.DeleteCommand = new AsyncRelayCommand(async () => await DeleteTaskAsync(item));

            return item;
        }

        private void OnTaskStatusChanged(TaskDisplayItem displayItem)
        {
            if (_suppressStatusSave)
                return;

            _ = SaveStatusAsync(displayItem);
        }

        public async Task SaveStatusAsync(TaskDisplayItem displayItem)
        {
            try
            {
                if (!displayItem.CanEdit)
                {
                    _dialogService.ShowWarning("You can edit only tasks assigned to you.", "Access denied");
                    var dbTask = _dataService.Tasks.FirstOrDefault(t => t.Id == displayItem.Id);
                    if (dbTask != null)
                    {
                        _suppressStatusSave = true;
                        displayItem.Status = _taskService.MapStatusToString(dbTask.Status);
                        _suppressStatusSave = false;
                    }
                    return;
                }

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
                if (!displayItem.CanEdit)
                {
                    _dialogService.ShowWarning("You can edit only tasks assigned to you.", "Access denied");
                    return;
                }

                var dbTask = _dataService.Tasks.Find(t => t.Id == displayItem.Id);
                if (dbTask != null)
                {
                    dbTask.Title = displayItem.Title;
                    dbTask.Description = displayItem.Description;
                    dbTask.Status = _taskService.MapStringToStatus(displayItem.Status);
                    dbTask.Priority = (TaskPriority)displayItem.Priority;
                    dbTask.Deadline = displayItem.Deadline;
                    dbTask.AssigneeId = displayItem.AssigneeId;

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

            if (!task.CanDelete)
            {
                _dialogService.ShowWarning("You can delete only tasks assigned to you.", "Access denied");
                return;
            }

            if (!_dialogService.Confirm($"Are you sure you want to delete '{task.Title}'?", "Confirm Delete"))
                return;

            try
            {
                if (_timeTrackingService.HasActiveSession && _timeTrackingService.ActiveTaskId == task.Id)
                {
                    await _timeTrackingService.StopActiveAsync("Timer stopped because task was deleted");
                }

                var success = await _dataService.DeleteTaskAsync(task.Id);
                if (success)
                {
                    _allTasks.Remove(task);
                    Tasks.Remove(task);
                    _activeTimer.Stop();
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

            if (SelectedSortOption == "My Tasks")
                filtered = filtered.Where(t => t.AssigneeId == ApiClient.Instance.UserId);

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
                "My Tasks" => filtered.OrderByDescending(t => t.Priority),
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
