using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Data;
using diplom.Models;
using diplom.Models.enums;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace diplom.viewmodels
{
    public class TaskDisplayItem : ObservableObject
    {
        public int Id { get; set; }
        
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _priority;
        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        private string _status = "To Do";
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    // Trigger save when status changes
                    OnStatusChanged?.Invoke(this);
                }
            }
        }

        private string _timeSpentFormatted = "00:00:00";
        public string TimeSpentFormatted
        {
            get => _timeSpentFormatted;
            set => SetProperty(ref _timeSpentFormatted, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string ProjectName { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public DateTime? Deadline { get; set; }

        public IRelayCommand ToggleTimerCommand { get; set; }
        public IRelayCommand EditCommand { get; set; }
        public IRelayCommand DeleteCommand { get; set; }
        
        // Callback when status changes
        public Action<TaskDisplayItem>? OnStatusChanged { get; set; }
    }

    public class StringIsNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TasksViewModel : ObservableObject
    {
        // Event to request opening dialog from View
        public event EventHandler RequestOpenDialog;
        public event EventHandler<TaskDisplayItem> RequestEditTask;

        public ObservableCollection<TaskDisplayItem> Tasks { get; set; } = new();
        public ObservableCollection<Project> AvailableProjects { get; set; } = new();

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterTasks();
                }
            }
        }

        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    FilterTasks();
                }
            }
        }

        private string _selectedSortOption = "Priority";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    FilterTasks();
                }
            }
        }

        public string[] SortOptions { get; } = new[] { "Priority", "Status", "Deadline", "Title" };

        // === Create Task Dialog Properties ===
        private bool _isCreateDialogOpen;
        public bool IsCreateDialogOpen
        {
            get => _isCreateDialogOpen;
            set => SetProperty(ref _isCreateDialogOpen, value);
        }

        private string _newTaskTitle = string.Empty;
        public string NewTaskTitle
        {
            get => _newTaskTitle;
            set => SetProperty(ref _newTaskTitle, value);
        }

        private string _newTaskDescription = string.Empty;
        public string NewTaskDescription
        {
            get => _newTaskDescription;
            set => SetProperty(ref _newTaskDescription, value);
        }

        private TaskPriority _newTaskPriority = TaskPriority.Medium;
        public TaskPriority NewTaskPriority
        {
            get => _newTaskPriority;
            set => SetProperty(ref _newTaskPriority, value);
        }

        private Project _selectedProject;
        public Project SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);
        }

        private DateTime? _newTaskDeadline;
        public DateTime? NewTaskDeadline
        {
            get => _newTaskDeadline;
            set => SetProperty(ref _newTaskDeadline, value);
        }

        private double _newTaskEstimatedHours;
        public double NewTaskEstimatedHours
        {
            get => _newTaskEstimatedHours;
            set => SetProperty(ref _newTaskEstimatedHours, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // === Commands ===
        public IRelayCommand CreateTaskCommand { get; }
        public IRelayCommand OpenCreateDialogCommand { get; }
        public IRelayCommand CloseCreateDialogCommand { get; }
        public IRelayCommand<TaskDisplayItem> DeleteTaskCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        private ObservableCollection<TaskDisplayItem> _allTasks = new();
        private readonly AppDataService _dataService;

        public TasksViewModel()
        {
            _dataService = AppDataService.Instance;

            CreateTaskCommand = new RelayCommand(OpenCreateDialog);
            OpenCreateDialogCommand = new RelayCommand(OpenCreateDialog);
            CloseCreateDialogCommand = new RelayCommand(CloseCreateDialog);
            DeleteTaskCommand = new AsyncRelayCommand<TaskDisplayItem>(DeleteTaskAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);

            // Load from cache immediately
            LoadFromCache();
        }

        private void LoadFromCache()
        {
            LoadProjectsFromCache();
            LoadTasksFromCache();
        }

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

        private async Task RefreshDataAsync()
        {
            IsLoading = true;
            try
            {
                await _dataService.RefreshTasksAsync();
                await _dataService.RefreshProjectsAsync();
                LoadFromCache();
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
                Status = MapStatusToString(task.Status),
                TimeSpentFormatted = FormatTimeSpan(timeSpent),
                ProjectName = task.Project?.Title ?? "No Project",
                ProjectId = task.ProjectId,
                Deadline = task.Deadline,
                IsActive = false
            };

            item.ToggleTimerCommand = new RelayCommand(() =>
            {
                item.IsActive = !item.IsActive;
            });

            // Edit command - opens edit dialog
            item.EditCommand = new RelayCommand(() =>
            {
                RequestEditTask?.Invoke(this, item);
            });

            // Delete command - deletes with confirmation
            item.DeleteCommand = new AsyncRelayCommand(async () =>
            {
                await DeleteTaskAsync(item);
            });

            // Subscribe to status changes to save to DB
            item.OnStatusChanged = OnTaskStatusChanged;

            return item;
        }

        private void OnTaskStatusChanged(TaskDisplayItem displayItem)
        {
            _ = SaveStatusAsync(displayItem);
        }

        private async Task SaveStatusAsync(TaskDisplayItem displayItem)
        {
            try
            {
                var task = _dataService.Tasks.FirstOrDefault(t => t.Id == displayItem.Id);
                if (task != null)
                {
                    task.Status = MapStringToStatus(displayItem.Status);
                    await _dataService.UpdateTaskAsync(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string MapStatusToString(AppTaskStatus status)
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

        private AppTaskStatus MapStringToStatus(string status)
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

        private string FormatTimeSpan(TimeSpan ts)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }

        private void OpenCreateDialog()
        {
            ResetCreateForm();
            IsCreateDialogOpen = true;
            RequestOpenDialog?.Invoke(this, EventArgs.Empty);
        }

        private void CloseCreateDialog()
        {
            IsCreateDialogOpen = false;
        }

        public void ResetCreateForm()
        {
            NewTaskTitle = string.Empty;
            NewTaskDescription = string.Empty;
            NewTaskPriority = TaskPriority.Medium;
            NewTaskDeadline = null;
            NewTaskEstimatedHours = 0;
            if (AvailableProjects.Any())
            {
                SelectedProject = AvailableProjects.First();
            }
        }

        public void ExecuteCreateTask()
        {
            _ = CreateTaskAsync();
        }

        private async Task CreateTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle))
            {
                MessageBox.Show("Task title is required", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedProject == null)
            {
                MessageBox.Show("Please select a project", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    EstimatedHours = NewTaskEstimatedHours
                };

                var created = await _dataService.CreateTaskAsync(newTask);
                var displayItem = MapToDisplayItem(created, TimeSpan.Zero);

                _allTasks.Insert(0, displayItem);
                Tasks.Insert(0, displayItem);

                CloseCreateDialog();
                ResetCreateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteTaskAsync(TaskDisplayItem task)
        {
            if (task == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{task.Title}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

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
                MessageBox.Show($"Error deleting task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTasks()
        {
            Tasks.Clear();

            var filtered = _allTasks.AsEnumerable();

            // Filter by status
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                filtered = filtered.Where(t => t.Status == SelectedStatusFilter);
            }

            // Filter by search query
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.ToLower();
                filtered = filtered.Where(t =>
                    t.Title.ToLower().Contains(query) ||
                    t.Description.ToLower().Contains(query) ||
                    t.ProjectName.ToLower().Contains(query));
            }

            // Apply sorting
            IOrderedEnumerable<TaskDisplayItem> sorted = SelectedSortOption switch
            {
                "Status" => filtered
                    .OrderBy(t => GetStatusOrder(t.Status))
                    .ThenByDescending(t => t.Priority),
                "Deadline" => filtered
                    .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                    .ThenByDescending(t => t.Priority),
                "Title" => filtered
                    .OrderBy(t => t.Title)
                    .ThenByDescending(t => t.Priority),
                _ => filtered.OrderByDescending(t => t.Priority) // Default: Priority
            };

            foreach (var task in sorted)
            {
                Tasks.Add(task);
            }
        }

        private int GetStatusOrder(string status)
        {
            // Order: In Progress (active work first), To Do, On Hold, Done
            return status switch
            {
                "In Progress" => 0,
                "To Do" => 1,
                "On Hold" => 2,
                "Done" => 3,
                _ => 4
            };
        }
    }
}