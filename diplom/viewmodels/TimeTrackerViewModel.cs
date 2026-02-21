using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace diplom.viewmodels
{
    public class TimeTrackerViewModel : ObservableObject
    {
        private readonly AppDataService _dataService;
        private readonly ITimeTrackingService _timeTrackingService;
        private DispatcherTimer _timer;

        private string _elapsedTime = "00:00:00";
        public string ElapsedTime
        {
            get => _elapsedTime;
            set => SetProperty(ref _elapsedTime, value);
        }

        private bool _isTimerRunning;
        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            set
            {
                SetProperty(ref _isTimerRunning, value);
                OnPropertyChanged(nameof(StartButtonText));
            }
        }

        public string StartButtonText => IsTimerRunning ? "Stop" : "Start";

        private TrackerTaskItem _selectedTask;
        public TrackerTaskItem SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public ObservableCollection<TrackerTaskItem> AvailableTasks { get; } = new ObservableCollection<TrackerTaskItem>();
        public ObservableCollection<TimeLogEntry> TodayLogs { get; } = new ObservableCollection<TimeLogEntry>();

        public ICommand ToggleTimerCommand { get; }

        public TimeTrackerViewModel()
        {
            _dataService = AppDataService.Instance;
            _timeTrackingService = TimeTrackingService.Instance;

            ToggleTimerCommand = new RelayCommand(ToggleTimer);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            // Subscribe to data updates
            _dataService.DataLoaded += LoadDataFromCache;

            if (_dataService.IsLoaded)
            {
                LoadDataFromCache();
            }

            RestoreActiveSessionFromService();
        }

        private void LoadDataFromCache()
        {
            AvailableTasks.Clear();
            var userId = ApiClient.Instance.UserId;
            foreach (var task in _dataService.Tasks.Where(t => t.Status != Models.enums.AppTaskStatus.Done && t.AssigneeId == userId))
            {
                AvailableTasks.Add(new TrackerTaskItem
                {
                    Id = task.Id,
                    Title = task.Title,
                    ProjectName = task.Project?.Title ?? "No Project"
                });
            }

            TodayLogs.Clear();
            foreach (var entry in _dataService.TimeEntries.OrderBy(e => e.StartTime))
            {
                var task = entry.Task ?? _dataService.Tasks.FirstOrDefault(t => t.Id == entry.TaskId);
                UpsertTodayLog(
                    entry.TaskId,
                    task?.Title ?? $"Task #{entry.TaskId}",
                    task?.Project?.Title ?? string.Empty,
                    entry.Duration,
                    entry.StartTime.ToLocalTime(),
                    entry.EndTime?.ToLocalTime());
            }

            RestoreActiveSessionFromService();
        }

        private void RestoreActiveSessionFromService()
        {
            if (!_timeTrackingService.HasActiveSession || !_timeTrackingService.ActiveTaskId.HasValue || !_timeTrackingService.ActiveStartTimeLocal.HasValue)
            {
                if (IsTimerRunning)
                {
                    IsTimerRunning = false;
                    ElapsedTime = "00:00:00";
                    _timer.Stop();
                }
                return;
            }

            var activeTaskId = _timeTrackingService.ActiveTaskId.Value;

            // Try pick from already loaded tasks (assigned to current user)
            var selected = AvailableTasks.FirstOrDefault(t => t.Id == activeTaskId);
            if (selected == null)
            {
                var task = _dataService.Tasks.FirstOrDefault(t => t.Id == activeTaskId);
                if (task != null)
                {
                    selected = new TrackerTaskItem
                    {
                        Id = task.Id,
                        Title = task.Title,
                        ProjectName = task.Project?.Title ?? "No Project"
                    };
                    AvailableTasks.Insert(0, selected);
                }
            }

            if (selected != null)
                SelectedTask = selected;

            IsTimerRunning = true;

            var elapsed = DateTime.Now - _timeTrackingService.ActiveStartTimeLocal.Value;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            ElapsedTime = _timeTrackingService.FormatTimeSpan(elapsed);

            if (!_timer.IsEnabled)
                _timer.Start();
        }

        private void ToggleTimer()
        {
            if (IsTimerRunning)
            {
                StopTimer();
            }
            else
            {
                StartTimer();
            }
        }

        private void StartTimer()
        {
            if (SelectedTask == null) return;

            try
            {
                _timeTrackingService.Start(SelectedTask.Id);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to start timer: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsTimerRunning = true;
            _timer.Start();
        }

        private async void StopTimer()
        {
            _timer.Stop();

            if (SelectedTask != null)
            {
                try
                {
                    var entry = await _timeTrackingService.StopActiveAsync();
                    if (entry?.EndTime != null)
                    {
                        var elapsed = entry.EndTime.Value - entry.StartTime;
                        UpsertTodayLog(
                            SelectedTask.Id,
                            SelectedTask.Title,
                            SelectedTask.ProjectName,
                            elapsed,
                            entry.StartTime.ToLocalTime(),
                            entry.EndTime.Value.ToLocalTime());
                    }
                }
                catch (Exception ex)
                {
                    // Keep UX consistent with existing tracker style: avoid hard crash but show reason.
                    System.Windows.MessageBox.Show($"Failed to stop timer: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    _timer.Start();
                    return;
                }
            }

            IsTimerRunning = false;
            ElapsedTime = "00:00:00";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_timeTrackingService.ActiveStartTimeLocal.HasValue)
                return;

            var elapsed = DateTime.Now - _timeTrackingService.ActiveStartTimeLocal.Value;
            ElapsedTime = _timeTrackingService.FormatTimeSpan(elapsed);
        }

        private void UpsertTodayLog(int taskId, string taskTitle, string projectName, TimeSpan duration, DateTime startLocal, DateTime? endLocal)
        {
            var existing = TodayLogs.FirstOrDefault(l => l.TaskId == taskId);
            if (existing == null)
            {
                TodayLogs.Insert(0, new TimeLogEntry
                {
                    TaskId = taskId,
                    TaskTitle = taskTitle,
                    ProjectName = projectName,
                    TotalDuration = duration,
                    Duration = _timeTrackingService.FormatTimeSpan(duration),
                    StartTime = startLocal.ToString("HH:mm"),
                    EndTime = endLocal?.ToString("HH:mm") ?? "..."
                });
                return;
            }

            existing.TotalDuration += duration;
            existing.Duration = _timeTrackingService.FormatTimeSpan(existing.TotalDuration);

            if (DateTime.TryParse(existing.StartTime, out var existingStart))
            {
                var minStart = existingStart.TimeOfDay <= startLocal.TimeOfDay ? existingStart : startLocal;
                existing.StartTime = minStart.ToString("HH:mm");
            }
            else
            {
                existing.StartTime = startLocal.ToString("HH:mm");
            }

            if (endLocal.HasValue)
                existing.EndTime = endLocal.Value.ToString("HH:mm");
        }
    }

    public class TrackerTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ProjectName { get; set; }
    }

    public class TimeLogEntry
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string ProjectName { get; set; }
        public string Duration { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}
