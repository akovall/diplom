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
            }
        }
    }
}
