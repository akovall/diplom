using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Models.enums;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace diplom.viewmodels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly AppDataService _dataService;
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly DispatcherTimer _uiTimer;

        private const double TargetWorkHoursPerDay = 8.0;

        private string _workedToday = "00:00";
        public string WorkedToday
        {
            get => _workedToday;
            set => SetProperty(ref _workedToday, value);
        }

        private double _workedTodayProgress;
        public double WorkedTodayProgress
        {
            get => _workedTodayProgress;
            set => SetProperty(ref _workedTodayProgress, value);
        }

        private int _tasksDone;
        public int TasksDone
        {
            get => _tasksDone;
            set => SetProperty(ref _tasksDone, value);
        }

        private int _tasksInProgress;
        public int TasksInProgress
        {
            get => _tasksInProgress;
            set => SetProperty(ref _tasksInProgress, value);
        }

        private double _productivity;
        public double Productivity
        {
            get => _productivity;
            set => SetProperty(ref _productivity, value);
        }

        private string _productivityText = "0%";
        public string ProductivityText
        {
            get => _productivityText;
            set => SetProperty(ref _productivityText, value);
        }

        private string _productivityDeltaText = string.Empty;
        public string ProductivityDeltaText
        {
            get => _productivityDeltaText;
            set => SetProperty(ref _productivityDeltaText, value);
        }

        private Brush _productivityForeground = Brushes.Gray;
        public Brush ProductivityForeground
        {
            get => _productivityForeground;
            set => SetProperty(ref _productivityForeground, value);
        }

        private string _currentActivityTitle = "No active task";
        public string CurrentActivityTitle
        {
            get => _currentActivityTitle;
            set => SetProperty(ref _currentActivityTitle, value);
        }

        private string _currentActivityProject = string.Empty;
        public string CurrentActivityProject
        {
            get => _currentActivityProject;
            set => SetProperty(ref _currentActivityProject, value);
        }

        private string _currentActivityTimer = "00:00:00";
        public string CurrentActivityTimer
        {
            get => _currentActivityTimer;
            set => SetProperty(ref _currentActivityTimer, value);
        }

        private bool _hasActiveSession;
        public bool HasActiveSession
        {
            get => _hasActiveSession;
            set => SetProperty(ref _hasActiveSession, value);
        }

        public ObservableCollection<UrgentTaskItem> UrgentTasks { get; } = new();
        public ObservableCollection<RecentTaskItem> RecentTasks { get; } = new();
        public ObservableCollection<DayActivity> WeeklyActivity { get; } = new();

        public IAsyncRelayCommand StopActiveTimerCommand { get; }

        public DashboardViewModel()
        {
            _dataService = AppDataService.Instance;
            _timeTrackingService = TimeTrackingService.Instance;

            StopActiveTimerCommand = new AsyncRelayCommand(StopActiveTimerAsync, () => HasActiveSession);

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uiTimer.Tick += (_, _) => UpdateCurrentActivity();
            _uiTimer.Start();

            _dataService.DataLoaded += LoadDashboardData;

            if (_dataService.IsLoaded)
                LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            var todayWorked = _dataService.GetTodayWorkedTime();
            WorkedToday = $"{(int)todayWorked.TotalHours:D2}:{todayWorked.Minutes:D2}";
            WorkedTodayProgress = Math.Clamp(todayWorked.TotalHours / TargetWorkHoursPerDay * 100.0, 0, 100);

            TasksInProgress = _dataService.GetTasksInProgressCount();
            TasksDone = _dataService.GetTasksDoneCount();

            LoadSmartProductivity();

            UrgentTasks.Clear();
            foreach (var task in _dataService.GetTopUrgentTasks(3))
            {
                UrgentTasks.Add(new UrgentTaskItem
                {
                    Title = task.Title,
                    DeadlineText = FormatDeadline(task.Deadline)
                });
            }

            RecentTasks.Clear();
            foreach (var task in _dataService.GetRecentTasks(5))
            {
                RecentTasks.Add(new RecentTaskItem
                {
                    Title = task.Title,
                    ProjectName = task.Project?.Title ?? "No Project",
                    Status = MapStatus(task.Status),
                    Priority = MapPriority(task.Priority),
                    TimeSpent = GetTaskTimeSpent(task)
                });
            }

            LoadWeeklyActivity();
            UpdateCurrentActivity();
        }

        private void LoadSmartProductivity()
        {
            var weekStart = AppDataService.GetWeekStartLocal(DateTime.Now);
            var current = _dataService.GetSmartProductivityForWeekLocal(weekStart);
            var previous = _dataService.GetSmartProductivityForWeekLocal(weekStart.AddDays(-7));

            Productivity = current.ScorePercent;
            ProductivityText = $"{Productivity}%";

            var delta = Math.Round(current.ScorePercent - previous.ScorePercent, 1);
            if (previous.TotalTasks == 0 && current.TotalTasks == 0)
            {
                ProductivityDeltaText = string.Empty;
            }
            else
            {
                var sign = delta > 0 ? "+" : "";
                ProductivityDeltaText = $"vs last week: {sign}{delta}%";
            }

            // Color thresholds: >=70 green, <40 red, else theme-neutral gray.
            ProductivityForeground = CreateFrozenBrushForProductivity(Productivity);
        }

        private static Brush CreateFrozenBrushForProductivity(double productivityPercent)
        {
            SolidColorBrush brush;
            if (productivityPercent >= 70.0)
                brush = new SolidColorBrush(Color.FromRgb(0x38, 0xA1, 0x69));
            else if (productivityPercent < 40.0)
                brush = new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));
            else
                brush = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));

            if (brush.CanFreeze)
                brush.Freeze();

            return brush;
        }

        private void UpdateCurrentActivity()
        {
            HasActiveSession = _timeTrackingService.HasActiveSession;
            StopActiveTimerCommand.NotifyCanExecuteChanged();

            if (!_timeTrackingService.HasActiveSession || !_timeTrackingService.ActiveTaskId.HasValue || !_timeTrackingService.ActiveStartTimeLocal.HasValue)
            {
                CurrentActivityTitle = "No active task";
                CurrentActivityProject = string.Empty;
                CurrentActivityTimer = "00:00:00";
                return;
            }

            var taskId = _timeTrackingService.ActiveTaskId.Value;
            var task = _dataService.Tasks.FirstOrDefault(t => t.Id == taskId);

            CurrentActivityTitle = task?.Title ?? $"Task #{taskId}";
            CurrentActivityProject = task?.Project?.Title != null ? $"Project: {task.Project.Title}" : string.Empty;

            var elapsed = DateTime.Now - _timeTrackingService.ActiveStartTimeLocal.Value;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            CurrentActivityTimer = _timeTrackingService.FormatTimeSpan(elapsed);
        }

        private async Task StopActiveTimerAsync()
        {
            await _timeTrackingService.StopActiveAsync();
            await _dataService.RefreshTasksAsync();
            UpdateCurrentActivity();
        }

        private void LoadWeeklyActivity()
        {
            WeeklyActivity.Clear();

            var days = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд" };
            var today = (int)DateTime.Today.DayOfWeek;
            if (today == 0) today = 7; // Sunday = 7

            for (int i = 0; i < 7; i++)
            {
                var isToday = (i + 1) == today;
                var hours = isToday ? _dataService.GetTodayWorkedTime().TotalHours : 0;

                WeeklyActivity.Add(new DayActivity
                {
                    DayName = days[i],
                    Hours = Math.Round(hours, 1),
                    IsToday = isToday,
                    BarHeight = Math.Max(10, hours * 15)
                });
            }
        }

        private static string FormatDeadline(DateTime? deadline)
        {
            if (!deadline.HasValue)
                return string.Empty;

            var local = deadline.Value;
            var today = DateTime.Today;

            if (local.Date == today)
                return $"Today, {local:HH:mm}";
            if (local.Date == today.AddDays(1))
                return $"Tomorrow, {local:HH:mm}";

            return local.ToString("dd.MM.yyyy, HH:mm");
        }

        private string GetTaskTimeSpent(TaskItem task)
        {
            if (task.TimeEntries == null || !task.TimeEntries.Any())
                return "00:00";
            
            var total = TimeSpan.FromTicks(task.TimeEntries.Where(e => e.EndTime.HasValue).Sum(e => e.Duration.Ticks));
            return $"{(int)total.TotalHours:D2}:{total.Minutes:D2}";
        }

        private static string MapStatus(AppTaskStatus status)
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

        private static string MapPriority(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => "Low",
                TaskPriority.Medium => "Medium",
                TaskPriority.High => "High",
                TaskPriority.Critical => "Critical",
                _ => "Medium"
            };
        }
    }

    public class RecentTaskItem
    {
        public string Title { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public string TimeSpent { get; set; } = "00:00";
    }

    public class DayActivity
    {
        public string DayName { get; set; } = "";
        public double Hours { get; set; }
        public bool IsToday { get; set; }
        public double BarHeight { get; set; }
    }

    public class UrgentTaskItem
    {
        public string Title { get; set; } = string.Empty;
        public string DeadlineText { get; set; } = string.Empty;
    }
}
