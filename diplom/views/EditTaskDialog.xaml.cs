using diplom.viewmodels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace diplom.views
{
    public partial class EditTaskDialog : Window
    {
        public TaskDisplayItem Task { get; private set; }
        public bool WasSaved { get; private set; } = false;

        public EditTaskDialog(TaskDisplayItem task)
        {
            InitializeComponent();
            Task = task;
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            TitleTextBox.Text = Task.Title;
            DescriptionTextBox.Text = Task.Description;
            StatusComboBox.SelectedItem = Task.Status;
            DeadlinePicker.SelectedDate = Task.Deadline;

            // Set priority
            foreach (ComboBoxItem item in PriorityComboBox.Items)
            {
                if (item.Tag?.ToString() == Task.Priority.ToString())
                {
                    PriorityComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Назва задачі обов'язкова", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update task properties
            Task.Title = TitleTextBox.Text.Trim();
            Task.Description = DescriptionTextBox.Text?.Trim() ?? "";
            Task.Status = StatusComboBox.SelectedItem?.ToString() ?? "To Do";
            Task.Deadline = DeadlinePicker.SelectedDate;

            if (PriorityComboBox.SelectedItem is ComboBoxItem priorityItem)
            {
                Task.Priority = int.Parse(priorityItem.Tag?.ToString() ?? "1");
            }

            WasSaved = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            WasSaved = false;
            DialogResult = false;
            Close();
        }
    }
}
