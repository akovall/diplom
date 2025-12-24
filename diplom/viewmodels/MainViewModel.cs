using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

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

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            CurrentView = new DashboardViewModel();
            NavigateCommand = new RelayCommand<string>(OnNavigate);
        }

        private void OnNavigate(string destination)
        {
            switch (destination)
            {
                case "Dashboard":
                    CurrentView = new DashboardViewModel();
                    break;
                case "Tasks":
                    CurrentView = new TasksViewModel();
                    break;
                case "Projects":
                    CurrentView = new ProjectsViewModel();
                    break;
            }
        }
    }
}
