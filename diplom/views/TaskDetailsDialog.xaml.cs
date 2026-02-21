using diplom.viewmodels;
using System.Windows;
using System.Windows.Input;

namespace diplom.views
{
    public partial class TaskDetailsDialog : Window
    {
        public TaskDisplayItem Task { get; }

        public TaskDetailsDialog(TaskDisplayItem task)
        {
            InitializeComponent();
            Task = task;
            DataContext = Task;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

