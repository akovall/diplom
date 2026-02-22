using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using diplom.Services;
using System.Windows.Media;
using System.Windows.Threading;
using System;

namespace diplom.viewmodels
{
    public class MainViewModel : ObservableObject
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _currentPageTitle = "Dashboard";
        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        private string _themeIcon = "🌙";
        public string ThemeIcon
        {
            get => _themeIcon;
            set => SetProperty(ref _themeIcon, value);
        }

        // Current user info for the header
        public string CurrentUserName => ApiClient.Instance.FullName;
        public string CurrentUserRole => ApiClient.Instance.Role;
        public bool IsAdminOrManager => ApiClient.Instance.Role is "Admin" or "Manager";

        private Brush _currentUserStatusBrush = new SolidColorBrush(Color.FromRgb(0xF6, 0xAD, 0x55)); // yellow
        public Brush CurrentUserStatusBrush
        {
            get => _currentUserStatusBrush;
            set => SetProperty(ref _currentUserStatusBrush, value);
        }

        private readonly DispatcherTimer _headerStatusTimer;
        private readonly DispatcherTimer _presenceTimer;

        public string UserInitials
        {
            get
            {
                var name = ApiClient.Instance.FullName;
                if (string.IsNullOrWhiteSpace(name)) return "?";
                var parts = name.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : name[0].ToString().ToUpper();
            }
        }
        public MainViewModel()
        {
            CurrentView = new DashboardViewModel();
            NavigateCommand = new RelayCommand<string>(OnNavigate);

            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            _headerStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _headerStatusTimer.Tick += (_, _) => UpdateHeaderStatus();
            _headerStatusTimer.Start();
            UpdateHeaderStatus();

            _presenceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _presenceTimer.Tick += async (_, _) => await SendPresenceHeartbeatAsync();
            _presenceTimer.Start();
            _ = SendPresenceHeartbeatAsync();

            _ = InitializeRealTimeAsync();
        }

        private void UpdateHeaderStatus()
        {
            // MVP: show green when current user has active timer, else yellow.
            var hasActive = TimeTrackingService.Instance.HasActiveSession;
            CurrentUserStatusBrush = hasActive
                ? new SolidColorBrush(Color.FromRgb(0x48, 0xBB, 0x78)) // green
                : new SolidColorBrush(Color.FromRgb(0xF6, 0xAD, 0x55)); // yellow
        }

        private static async System.Threading.Tasks.Task SendPresenceHeartbeatAsync()
        {
            if (!ApiClient.Instance.IsAuthenticated)
                return;

            await ApiClient.Instance.PostAsync("/api/presence/heartbeat");
        }

        private async System.Threading.Tasks.Task InitializeRealTimeAsync()
        {
            await RealTimeService.Instance.EnsureStartedAsync();
            RealTimeService.Instance.TimeEntryChanged += (_, _) =>
            {
                _ = AppDataService.Instance.RefreshTasksAsync();
                _ = AppDataService.Instance.RefreshTimeEntriesTodayAsync();
            };

            RealTimeService.Instance.TaskChanged += taskId =>
            {
                _ = AppDataService.Instance.RefreshTasksAsync();
                _ = AppDataService.Instance.RefreshProjectsAsync();
            };
        }
        private void ToggleTheme()
        {
            if (ThemeService.CurrentTheme == "Light")
            {
                ThemeService.SetTheme("Dark");
                ThemeIcon = "☀️";
            }
            else
            {
                ThemeService.SetTheme("Light");
                ThemeIcon = "🌙";
            }
        }
        private void OnNavigate(string destination)
        {
            switch (destination)
            {
                case "Dashboard":
                    CurrentView = new DashboardViewModel();
                    CurrentPageTitle = "Dashboard";
                    break;
                case "Tasks":
                    CurrentView = new TasksViewModel();
                    CurrentPageTitle = "Tasks";
                    break;
                case "Projects":
                    CurrentView = new ProjectsViewModel();
                    CurrentPageTitle = "Projects";
                    break;
                case "TimeTracker":
                    CurrentView = new TimeTrackerViewModel();
                    CurrentPageTitle = "Time Tracker";
                    break;
                case "Settings":
                    CurrentView = new SettingsViewModel();
                    CurrentPageTitle = "Settings";
                    break;
                case "Users":
                    CurrentView = new UsersViewModel();
                    CurrentPageTitle = "Users";
                    break;
            }
        }
    }
}
