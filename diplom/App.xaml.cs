using diplom.Services;
using diplom.views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace diplom
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent auto-shutdown when LoginWindow closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Show login screen first
            var loginWindow = new LoginWindow();
            var result = loginWindow.ShowDialog();

            if (result != true || !loginWindow.IsLoggedIn)
            {
                Shutdown();
                return;
            }

            // Show splash screen while loading data
            var splash = new views.SplashScreen();
            splash.Show();

            // Load data and show main window
            LoadAndShowMainWindow(splash);
        }

        private async void LoadAndShowMainWindow(views.SplashScreen splash)
        {
            try
            {
                await AppDataService.Instance.LoadAllDataAsync();
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading data:\n{ex.Message}\n\nApplication will continue without data.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            // Now that MainWindow is shown, switch to normal shutdown behavior
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            splash.Close();
        }
    }
}
