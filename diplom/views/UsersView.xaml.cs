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
    }
}
