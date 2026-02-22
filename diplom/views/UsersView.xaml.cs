using System.Windows;
using System.Windows.Controls;
using diplom.viewmodels;
using diplom.Models;
using diplom.Services;

namespace diplom.views
{
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
        }

        private void Role_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // RemovedItems.Count > 0 ensures this isn't the initial binding/load
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count > 0 && 
                DataContext is UsersViewModel vm && (sender as FrameworkElement)?.DataContext is UserDisplayItem user)
            {
                vm.ChangeUserRoleCommand.Execute(user);
            }
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void DeactivateAccount_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UsersViewModel vm)
                return;

            if (sender is not MenuItem mi)
                return;

            if (mi.DataContext is not UserDisplayItem user)
                return;

            if (user.IsActive)
                vm.ToggleUserStatusCommand.Execute(user);
        }

        private void ActivateAccount_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UsersViewModel vm)
                return;

            if (sender is not MenuItem mi)
                return;

            if (mi.DataContext is not UserDisplayItem user)
                return;

            if (!user.IsActive)
                vm.ToggleUserStatusCommand.Execute(user);
        }

        private void AnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UsersViewModel vm)
                return;

            if (sender is not Button button)
                return;

            if (button.DataContext is not UserDisplayItem user)
                return;

            vm.OpenUserAnalyticsCommand.Execute(user);
        }

        private void JobTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not UsersViewModel vm)
                return;

            if ((sender as FrameworkElement)?.DataContext is not UserDisplayItem user)
                return;

            if (sender is not ComboBox comboBox)
                return;

            if (comboBox.SelectedItem is string selected &&
                selected == UsersViewModel.AddProfessionOption)
            {
                var title = PromptForProfession();
                if (string.IsNullOrWhiteSpace(title))
                {
                    comboBox.SelectedItem = null;
                    comboBox.Text = user.JobTitle;
                    return;
                }

                title = title.Trim();
                if (!vm.AvailableJobTitles.Contains(title))
                {
                    var insertIndex = vm.AvailableJobTitles.Count;
                    if (insertIndex > 0 && vm.AvailableJobTitles[insertIndex - 1] == UsersViewModel.AddProfessionOption)
                        insertIndex--;
                    vm.AvailableJobTitles.Insert(insertIndex, title);
                }

                user.JobTitle = title;
                comboBox.SelectedItem = title;
                comboBox.Text = title;
                vm.ChangeUserJobTitleCommand.Execute(user);
                return;
            }

            if (e.RemovedItems.Count > 0 || e.AddedItems.Count > 0)
                vm.ChangeUserJobTitleCommand.Execute(user);
        }

        private void JobTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UsersViewModel vm)
                return;

            if ((sender as FrameworkElement)?.DataContext is not UserDisplayItem user)
                return;

            vm.ChangeUserJobTitleCommand.Execute(user);
        }

        private static string? PromptForProfession()
        {
            var app = Application.Current;
            var bg = app?.TryFindResource("PrimaryBackground") as System.Windows.Media.Brush;
            var surface = app?.TryFindResource("SurfaceBrush") as System.Windows.Media.Brush;
            var primary = app?.TryFindResource("PrimaryText") as System.Windows.Media.Brush;
            var secondary = app?.TryFindResource("SecondaryText") as System.Windows.Media.Brush;
            var border = app?.TryFindResource("BorderBrush") as System.Windows.Media.Brush;
            var accent = app?.TryFindResource("AccentColor") as System.Windows.Media.Brush;

            var dialog = new Window
            {
                Title = LocalizationService.CurrentLanguage == "en" ? "Add profession" : "Додати професію",
                Width = 560,
                Height = 240,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.ToolWindow,
                Background = bg ?? System.Windows.Media.Brushes.White
            };

            var panel = new StackPanel { Margin = new Thickness(18) };
            var text = new TextBox
            {
                Height = 38,
                FontSize = 14,
                Margin = new Thickness(0, 10, 0, 14),
                Background = surface,
                Foreground = primary,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6)
            };
            var ok = new Button
            {
                Content = LocalizationService.CurrentLanguage == "en" ? "Add" : "Додати",
                Width = 110,
                Height = 34,
                IsDefault = true,
                Margin = new Thickness(0, 0, 8, 0),
                Background = accent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            var cancel = new Button
            {
                Content = LocalizationService.CurrentLanguage == "en" ? "Cancel" : "Скасувати",
                Width = 110,
                Height = 34,
                IsCancel = true,
                Background = surface,
                Foreground = primary,
                BorderBrush = border,
                BorderThickness = new Thickness(1)
            };

            ok.Click += (_, _) => dialog.DialogResult = true;

            panel.Children.Add(new TextBlock
            {
                Text = LocalizationService.CurrentLanguage == "en"
                    ? "Enter profession"
                    : "Введіть професію",
                Foreground = secondary,
                FontSize = 13
            });
            panel.Children.Add(text);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            actions.Children.Add(ok);
            actions.Children.Add(cancel);
            panel.Children.Add(actions);

            dialog.Content = panel;
            if (Application.Current?.MainWindow != null)
                dialog.Owner = Application.Current.MainWindow;

            return dialog.ShowDialog() == true ? text.Text : null;
        }
    }
}
