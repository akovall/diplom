using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace diplom.viewmodels
{
    public class ProjectDisplayItem : ObservableObject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public double Progress => TaskCount > 0 ? (double)CompletedTaskCount / TaskCount : 0;
        public string ProgressText => $"{CompletedTaskCount}/{TaskCount} tasks";
    }

    public class ProjectsViewModel : ObservableObject
    {
        public ObservableCollection<ProjectDisplayItem> Projects { get; set; }
        public string SearchQuery { get; set; }
        public IRelayCommand CreateProjectCommand { get; }

        public ProjectsViewModel()
        {
            Projects = new ObservableCollection<ProjectDisplayItem>
            {
                new ProjectDisplayItem 
                { 
                    Name = "Administration", 
                    Description = "System administration and infrastructure management tasks.",
                    Color = "#E53E3E",
                    TaskCount = 12,
                    CompletedTaskCount = 8
                },
                new ProjectDisplayItem 
                { 
                    Name = "Test", 
                    Description = "Quality assurance and testing automation project.",
                    Color = "#38A169",
                    TaskCount = 24,
                    CompletedTaskCount = 18
                },
                new ProjectDisplayItem 
                { 
                    Name = "Diplom", 
                    Description = "Diploma project - task management system development.",
                    Color = "#3182CE",
                    TaskCount = 15,
                    CompletedTaskCount = 6
                },
                new ProjectDisplayItem 
                { 
                    Name = "Marketing", 
                    Description = "Marketing campaigns and brand development initiatives.",
                    Color = "#D69E2E",
                    TaskCount = 8,
                    CompletedTaskCount = 3
                },
            };

            CreateProjectCommand = new RelayCommand(() => { });
        }
    }
}
