using System.Windows;
using System.Windows.Controls;
using diplom.viewmodels;
using diplom.Models;

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
    }
}
