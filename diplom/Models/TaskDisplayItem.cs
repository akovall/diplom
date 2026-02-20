using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace diplom.viewmodels
{
    public class TaskDisplayItem : ObservableObject
    {
        public int Id { get; set; }
        
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _priority;
        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        private string _status = "To Do";
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnStatusChanged?.Invoke(this);
                }
            }
        }

        private string _timeSpentFormatted = "00:00:00";
        public string TimeSpentFormatted
        {
            get => _timeSpentFormatted;
            set => SetProperty(ref _timeSpentFormatted, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string ProjectName { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public DateTime? Deadline { get; set; }
        public int? AssigneeId { get; set; }

        private DateTime? _activeStartTime;
        public DateTime? ActiveStartTime
        {
            get => _activeStartTime;
            set => SetProperty(ref _activeStartTime, value);
        }

        private TimeSpan _accumulatedTime;
        public TimeSpan AccumulatedTime
        {
            get => _accumulatedTime;
            set => SetProperty(ref _accumulatedTime, value);
        }

        public ICommand ToggleTimerCommand { get; set; }
        public IRelayCommand EditCommand { get; set; }
        public IRelayCommand DeleteCommand { get; set; }
        
        // Callback when status changes
        public Action<TaskDisplayItem> OnStatusChanged { get; set; }
    }
}
