using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using diplom.Services;

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
