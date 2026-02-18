using CommunityToolkit.Mvvm.ComponentModel;
using diplom.Models;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace diplom.viewmodels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly AppDataService _dataService;

        // Stats cards
        private string _workedToday = "00:00";
        public string WorkedToday
        {
            get => _workedToday;
            set => SetProperty(ref _workedToday, value);
        }

        private int _tasksInProgress;
        public int TasksInProgress
        {
            get => _tasksInProgress;
            set => SetProperty(ref _tasksInProgress, value);
        }

        private int _urgentTasks;
        public int UrgentTasks
        {
            get => _urgentTasks;
            set => SetProperty(ref _urgentTasks, value);
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

        // Recent tasks
        public ObservableCollection<RecentTaskItem> RecentTasks { get; } = new();

        // Weekly activity (for chart)
        public ObservableCollection<DayActivity> WeeklyActivity { get; } = new();

        public DashboardViewModel()
        {
            _dataService = AppDataService.Instance;
            
            // Subscribe to data updates
            _dataService.DataLoaded += LoadDashboardData;
            
            // Load initial data if already available
            if (_dataService.IsLoaded)
            {
                LoadDashboardData();
            }
        }

        private void LoadDashboardData()
        {
            // Worked today
            var todayWorked = _dataService.GetTodayWorkedTime();
            WorkedToday = $"{(int)todayWorked.TotalHours:D2}:{todayWorked.Minutes:D2}";

            // Tasks in progress
            TasksInProgress = _dataService.GetTasksInProgressCount();

            // Urgent tasks
            UrgentTasks = _dataService.GetUrgentTasksCount();

            // Productivity
            Productivity = _dataService.GetProductivityPercentage();
            ProductivityText = $"{Productivity}%";

            // Recent tasks
            RecentTasks.Clear();
            var recentTasks = _dataService.GetRecentTasks(5);
            foreach (var task in recentTasks)
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

            // Weekly activity (sample data for now - will be real later)
            LoadWeeklyActivity();
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
                    BarHeight = Math.Max(10, hours * 15) // Scale for visualization
                });
            }
        }

        private string GetTaskTimeSpent(TaskItem task)
        {
            if (task.TimeEntries == null || !task.TimeEntries.Any())
                return "00:00";
            
            var total = TimeSpan.FromTicks(task.TimeEntries.Sum(e => e.Duration.Ticks));
            return $"{(int)total.TotalHours:D2}:{total.Minutes:D2}";
        }

        private string MapStatus(Models.enums.AppTaskStatus status)
        {
            return status switch
            {
                Models.enums.AppTaskStatus.ToDo => "Pending",
                Models.enums.AppTaskStatus.InProgress => "In Progress",
                Models.enums.AppTaskStatus.OnHold => "On Hold",
                Models.enums.AppTaskStatus.Done => "Done",
                _ => "Pending"
            };
        }

        private string MapPriority(Models.enums.TaskPriority priority)
        {
            return priority switch
            {
                Models.enums.TaskPriority.Low => "Low",
                Models.enums.TaskPriority.Medium => "Medium",
                Models.enums.TaskPriority.High => "High",
                Models.enums.TaskPriority.Critical => "Critical",
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
}
