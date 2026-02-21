using diplom.viewmodels;
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
                vm.RequestViewTaskDetails += OnRequestViewTaskDetails;
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
                if (DataContext is TasksViewModel vm)
                {
                    await vm.SaveTaskEditAsync(task);
                }
            }
        }

        private void OnRequestViewTaskDetails(object sender, TaskDisplayItem task)
        {
            var dialog = new TaskDetailsDialog(task)
            {
                Owner = Window.GetWindow(this)
            };

            dialog.ShowDialog();
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
