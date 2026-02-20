using diplom.Models;
using diplom.Services;
using diplom.viewmodels;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace diplom.views
{
    public partial class EditTaskDialog : Window
    {
        private readonly ITimeTrackingService _timeTrackingService;
        public TaskDisplayItem Task { get; private set; }
        public bool WasSaved { get; private set; } = false;

        public EditTaskDialog(TaskDisplayItem task)
            : this(task, TimeTrackingService.Instance)
        {
        }

        public EditTaskDialog(TaskDisplayItem task, ITimeTrackingService timeTrackingService)
        {
            InitializeComponent();
            _timeTrackingService = timeTrackingService;
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

            // Set Assignees
            var isManagerOrAdmin = ApiClient.Instance.Role is "Admin" or "Manager";
            if (isManagerOrAdmin)
            {
                var users = AppDataService.Instance.Users;
                AssigneeComboBox.ItemsSource = users;
                AssigneeComboBox.SelectedItem = users.FirstOrDefault(u => u.Id == Task.AssigneeId);
            }
            else
            {
                // Employee: only self
                var users = new System.Collections.Generic.List<User> { 
                    new User { Id = ApiClient.Instance.UserId, FullName = ApiClient.Instance.FullName } 
                };
                AssigneeComboBox.ItemsSource = users;
                AssigneeComboBox.SelectedItem = users[0];
                AssigneeComboBox.IsEnabled = false;
            }

            ManualEntryDatePicker.SelectedDate = DateTime.Today;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Task title is required", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (AssigneeComboBox.SelectedItem is User assignee)
            {
                Task.AssigneeId = assignee.Id;
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

        private async void AddManualEntry_Click(object sender, RoutedEventArgs e)
        {
            var rawDuration = (ManualEntryDurationTextBox.Text ?? string.Empty).Trim();
            var parsed = double.TryParse(rawDuration, NumberStyles.Float, CultureInfo.CurrentCulture, out double durationHours)
                || double.TryParse(rawDuration.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out durationHours);

            if (!parsed || durationHours <= 0)
            {
                MessageBox.Show("Please enter a valid positive duration in hours (e.g. 1.5).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var date = ManualEntryDatePicker.SelectedDate ?? DateTime.Today;
            var comment = ManualEntryCommentTextBox.Text?.Trim() ?? "";
            var duration = TimeSpan.FromHours(durationHours);

            try
            {
                var created = await _timeTrackingService.AddManualEntryAsync(Task.Id, date, duration, comment);
                if (created != null)
                {
                    Task.AccumulatedTime += duration;
                    Task.TimeSpentFormatted = _timeTrackingService.FormatTimeSpan(Task.AccumulatedTime);
                    
                    MessageBox.Show("Time entry added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    ManualEntryDurationTextBox.Text = "";
                    ManualEntryCommentTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add time entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
