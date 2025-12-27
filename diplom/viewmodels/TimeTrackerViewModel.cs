using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace diplom.viewmodels
{
    public class TimeTrackerViewModel : ObservableObject
    {
        private DispatcherTimer _timer;
        private DateTime _startTime;

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

        private TaskItem _selectedTask;
        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public ObservableCollection<TaskItem> AvailableTasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TimeLogEntry> TodayLogs { get; } = new ObservableCollection<TimeLogEntry>();

        public ICommand ToggleTimerCommand { get; }

        public TimeTrackerViewModel()
        {
            ToggleTimerCommand = new RelayCommand(ToggleTimer);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            LoadSampleData();
        }

        private void LoadSampleData()
        {
            AvailableTasks.Add(new TaskItem { Id = 1, Title = "Implement login page", ProjectName = "Website Redesign" });
            AvailableTasks.Add(new TaskItem { Id = 2, Title = "Fix navigation bug", ProjectName = "Mobile App" });
            AvailableTasks.Add(new TaskItem { Id = 3, Title = "Write unit tests", ProjectName = "Backend API" });
            AvailableTasks.Add(new TaskItem { Id = 4, Title = "Design dashboard mockup", ProjectName = "Analytics Platform" });

            TodayLogs.Add(new TimeLogEntry
            {
                TaskTitle = "Code review",
                ProjectName = "Backend API",
                Duration = "1h 30m",
                StartTime = "09:00",
                EndTime = "10:30"
            });
            TodayLogs.Add(new TimeLogEntry
            {
                TaskTitle = "Team meeting",
                ProjectName = "General",
                Duration = "45m",
                StartTime = "11:00",
                EndTime = "11:45"
            });
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

            _startTime = DateTime.Now;
            IsTimerRunning = true;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
            IsTimerRunning = false;

            if (SelectedTask != null)
            {
                var elapsed = DateTime.Now - _startTime;
                TodayLogs.Insert(0, new TimeLogEntry
                {
                    TaskTitle = SelectedTask.Title,
                    ProjectName = SelectedTask.ProjectName,
                    Duration = FormatDuration(elapsed),
                    StartTime = _startTime.ToString("HH:mm"),
                    EndTime = DateTime.Now.ToString("HH:mm")
                });
            }

            ElapsedTime = "00:00:00";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _startTime;
            ElapsedTime = elapsed.ToString(@"hh\:mm\:ss");
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            return $"{duration.Minutes}m";
        }
    }

    public class TaskItem
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
