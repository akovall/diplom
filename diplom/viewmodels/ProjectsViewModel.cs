using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Models.enums;
using diplom.Services;
using diplom.views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace diplom.viewmodels
{
    public partial class ProjectDisplayItem : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        [ObservableProperty]
        private int _taskCount;

        [ObservableProperty]
        private int _completedTaskCount;

        public double Progress => TaskCount > 0 ? (double)CompletedTaskCount / TaskCount : 0;
        public string ProgressText => $"{CompletedTaskCount}/{TaskCount} задач виконано";

        partial void OnTaskCountChanged(int value)
        {
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }

        partial void OnCompletedTaskCountChanged(int value)
        {
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }

        public void UpdateStats(int taskCount, int completedTaskCount)
        {
            TaskCount = taskCount;
            CompletedTaskCount = completedTaskCount;
        }
    }

    public partial class ProjectsViewModel : ObservableObject
    {
        private readonly AppDataService _dataService;
        private readonly List<ProjectDisplayItem> _allProjects = new();

        public ObservableCollection<ProjectDisplayItem> Projects { get; } = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        public IRelayCommand CreateProjectCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand<ProjectDisplayItem> OpenProjectAnalyticsCommand { get; }

        private static readonly string[] _defaultColors =
        {
            "#E53E3E", "#38A169", "#3182CE", "#D69E2E", "#805AD5",
            "#DD6B20", "#0EA5E9", "#319795", "#D53F8C", "#718096"
        };

        public ProjectsViewModel()
        {
            _dataService = AppDataService.Instance;
            CreateProjectCommand = new RelayCommand(() => { });
            RefreshCommand = new AsyncRelayCommand(LoadProjectsAsync);
            OpenProjectAnalyticsCommand = new RelayCommand<ProjectDisplayItem>(OpenProjectAnalytics);

            _dataService.DataLoaded += OnDataLoaded;

            if (_dataService.IsLoaded)
                OnDataLoaded();
        }

        private void OnDataLoaded()
        {
            _allProjects.Clear();
            var colorIndex = 0;

            foreach (var project in _dataService.Projects)
            {
                var projectTasks = _dataService.Tasks.Where(t => t.ProjectId == project.Id).ToList();
                var taskCount = projectTasks.Count;
                var completedCount = projectTasks.Count(t => t.Status == AppTaskStatus.Done);

                _allProjects.Add(new ProjectDisplayItem
                {
                    Id = project.Id,
                    Name = project.Title,
                    Description = project.Description ?? string.Empty,
                    Color = _defaultColors[colorIndex % _defaultColors.Length],
                    TaskCount = taskCount,
                    CompletedTaskCount = completedCount
                });

                colorIndex++;
            }

            ApplyFilter();
        }

        private async Task LoadProjectsAsync()
        {
            await _dataService.RefreshTasksAsync();
            await _dataService.RefreshProjectsAsync();
            OnDataLoaded();
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            var query = (SearchQuery ?? string.Empty).Trim().ToLowerInvariant();
            var filtered = _allProjects.Where(p =>
                string.IsNullOrEmpty(query) ||
                p.Name.ToLowerInvariant().Contains(query) ||
                p.Description.ToLowerInvariant().Contains(query));

            Projects.Clear();
            foreach (var item in filtered)
                Projects.Add(item);
        }

        private void OpenProjectAnalytics(ProjectDisplayItem? project)
        {
            if (project == null) return;
            if (ApiClient.Instance.Role is not ("Admin" or "Manager")) return;

            var dialog = new ProjectAnalyticsDialog
            {
                Owner = Application.Current?.MainWindow,
                DataContext = new ProjectAnalyticsViewModel(project.Id, project.Name)
            };

            dialog.Show();
        }
    }
}
