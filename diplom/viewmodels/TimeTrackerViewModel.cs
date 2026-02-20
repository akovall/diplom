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
        }

        private void LoadDataFromCache()
        {
            AvailableTasks.Clear();
            foreach (var task in _dataService.Tasks.Where(t => t.Status != Models.enums.AppTaskStatus.Done))
            {
                AvailableTasks.Add(new TrackerTaskItem
                {
                    Id = task.Id,
                    Title = task.Title,
                    ProjectName = task.Project?.Title ?? "No Project"
                });
            }

            TodayLogs.Clear();
            foreach (var entry in _dataService.TimeEntries)
            {
                TodayLogs.Add(new TimeLogEntry
                {
                    TaskTitle = entry.Task?.Title ?? "Unknown",
                    ProjectName = entry.Task?.Project?.Title ?? "",
                    Duration = _timeTrackingService.FormatTimeSpan(entry.Duration),
                    StartTime = entry.StartTime.ToString("HH:mm"),
                    EndTime = entry.EndTime?.ToString("HH:mm") ?? "..."
                });
            }
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
                        TodayLogs.Insert(0, new TimeLogEntry
                        {
                            TaskTitle = SelectedTask.Title,
                            ProjectName = SelectedTask.ProjectName,
                            Duration = _timeTrackingService.FormatTimeSpan(elapsed),
                            StartTime = entry.StartTime.ToLocalTime().ToString("HH:mm"),
                            EndTime = entry.EndTime.Value.ToLocalTime().ToString("HH:mm")
                        });
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
    }

    public class TrackerTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ProjectName { get; set; }
    }

    public class TimeLogEntry
    {
        public string TaskTitle { get; set; }
        public string ProjectName { get; set; }
        public string Duration { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}
