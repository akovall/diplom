using diplom.viewmodels;
using diplom.Services;
using System.Windows;
using System.Windows.Controls;

namespace diplom.views
{
    public partial class TasksView : UserControl
    {
        public TasksView()
        {
            InitializeComponent();
            Loaded += TasksView_Loaded;
        }

        private void TasksView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TasksViewModel vm)
            {
                vm.RequestOpenDialog += OnRequestOpenDialog;
                vm.RequestEditTask += OnRequestEditTask;
            }
        }

        private void OnRequestOpenDialog(object sender, System.EventArgs e)
        {
            if (DataContext is TasksViewModel vm)
            {
                var dialog = new CreateTaskDialog
                {
                    DataContext = vm,
                    Owner = Window.GetWindow(this)
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    vm.ExecuteCreateTask();
                }
                else
                {
                    vm.ResetCreateForm();
                }
            }
        }

        private async void OnRequestEditTask(object sender, TaskDisplayItem task)
        {
            var dialog = new EditTaskDialog(task)
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();

            if (result == true && dialog.WasSaved)
            {
                // Save changes to database
                try
                {
                    var dataService = AppDataService.Instance;
                    var dbTask = dataService.Tasks.Find(t => t.Id == task.Id);
                    
                    if (dbTask != null)
                    {
                        dbTask.Title = task.Title;
                        dbTask.Description = task.Description;
                        dbTask.Status = MapStringToStatus(task.Status);
                        dbTask.Priority = (diplom.Models.enums.TaskPriority)task.Priority;
                        dbTask.Deadline = task.Deadline;
                        
                        await dataService.UpdateTaskAsync(dbTask);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private diplom.Models.enums.AppTaskStatus MapStringToStatus(string status)
        {
            return status switch
            {
                "To Do" => diplom.Models.enums.AppTaskStatus.ToDo,
                "In Progress" => diplom.Models.enums.AppTaskStatus.InProgress,
                "On Hold" => diplom.Models.enums.AppTaskStatus.OnHold,
                "Done" => diplom.Models.enums.AppTaskStatus.Done,
                _ => diplom.Models.enums.AppTaskStatus.ToDo
            };
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void StatusFilter_All(object sender, RoutedEventArgs e)
        {
            if (DataContext is TasksViewModel vm)
                vm.SelectedStatusFilter = "All";
        }

        private void StatusFilter_ToDo(object sender, RoutedEventArgs e)
        {
            if (DataContext is TasksViewModel vm)
                vm.SelectedStatusFilter = "To Do";
        }

        private void StatusFilter_InProgress(object sender, RoutedEventArgs e)
        {
            if (DataContext is TasksViewModel vm)
                vm.SelectedStatusFilter = "In Progress";
        }

        private void StatusFilter_Done(object sender, RoutedEventArgs e)
        {
            if (DataContext is TasksViewModel vm)
                vm.SelectedStatusFilter = "Done";
        }
    }
}
