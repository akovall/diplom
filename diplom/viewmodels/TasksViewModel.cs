using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace diplom.viewmodels
{
    public class TaskDisplayItem : ObservableObject
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public string TimeSpentFormatted { get; set; } 

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public IRelayCommand ToggleTimerCommand { get; set; }
    }
    public class StringIsNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TasksViewModel : ObservableObject
    {
        public ObservableCollection<TaskDisplayItem> Tasks { get; set; }
        public string SearchQuery { get; set; }

        public TasksViewModel()
        {
            var rawTasks = new ObservableCollection<TaskDisplayItem>
            {
                new TaskDisplayItem { Title = "Fix authentication bug", Description = "Users cannot log in if the password contains special characters.", Priority = 3, Status = "In Progress", TimeSpentFormatted = "01:30:00", IsActive = true },
                new TaskDisplayItem { Title = "Dashboard layout", Description = "Need to create the KPI blocks and chart layout.", Priority = 2, Status = "To Do", TimeSpentFormatted = "00:00:00", IsActive = false },
                new TaskDisplayItem { Title = "Update documentation", Description = "Add descriptions for new API methods.", Priority = 1, Status = "To Do", TimeSpentFormatted = "04:15:00", IsActive = false },
                new TaskDisplayItem { Title = "Minor UI fixes", Description = "Move the button by 2 pixels.", Priority = 0, Status = "Done", TimeSpentFormatted = "00:45:00", IsActive = false },
            };

            var sorted = rawTasks.OrderByDescending(t => t.Priority).ToList();
            Tasks = new ObservableCollection<TaskDisplayItem>(sorted);

            foreach (var task in Tasks)
            {
                task.ToggleTimerCommand = new RelayCommand(() =>
                {
                    task.IsActive = !task.IsActive; 
                });
            }
        }
    }
}