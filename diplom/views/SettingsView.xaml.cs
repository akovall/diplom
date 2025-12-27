using System.Windows.Controls;
using System.Windows.Input;

namespace diplom.views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void ThemeToggle_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is viewmodels.SettingsViewModel vm)
            {
                vm.IsDarkTheme = !vm.IsDarkTheme;
            }
        }
    }
}
