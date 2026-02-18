using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace diplom.viewmodels
{
    public class ProjectDisplayItem : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public double Progress => TaskCount > 0 ? (double)CompletedTaskCount / TaskCount : 0;
        public string ProgressText => $"{CompletedTaskCount}/{TaskCount} tasks";
    }

    public partial class ProjectsViewModel : ObservableObject
    {
        private readonly AppDataService _dataService;

        public ObservableCollection<ProjectDisplayItem> Projects { get; set; } = new();

        [ObservableProperty]
        private string _searchQuery;

        public IRelayCommand CreateProjectCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        // Default color palette for projects
        private static readonly string[] _defaultColors =
        {
            "#E53E3E", "#38A169", "#3182CE", "#D69E2E", "#805AD5",
            "#DD6B20", "#E53E3E", "#319795", "#D53F8C", "#718096"
        };

        public ProjectsViewModel()
        {
            _dataService = AppDataService.Instance;
            CreateProjectCommand = new RelayCommand(() => { });
            RefreshCommand = new AsyncRelayCommand(LoadProjectsAsync);

            // Subscribe to data updates
            _dataService.DataLoaded += OnDataLoaded;
            
            if (_dataService.IsLoaded)
            {
                OnDataLoaded();
            }
        }

        private void OnDataLoaded()
        {
            Projects.Clear();
            int colorIndex = 0;
            foreach (var project in _dataService.Projects)
            {
                var taskCount = project.Tasks?.Count ?? 0;
                var completedCount = project.Tasks?.Count(t => t.Status == Models.enums.AppTaskStatus.Done) ?? 0;

                Projects.Add(new ProjectDisplayItem
                {
                    Id = project.Id,
                    Name = project.Title,
                    Description = project.Description ?? "",
                    Color = _defaultColors[colorIndex % _defaultColors.Length],
                    TaskCount = taskCount,
                    CompletedTaskCount = completedCount
                });
                colorIndex++;
            }
        }

        private async Task LoadProjectsAsync()
        {
            await _dataService.RefreshProjectsAsync();
            OnDataLoaded();
        }
    }
}
