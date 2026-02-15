using diplom.Services;
using diplom.views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace diplom
{
    public partial class App : Application
    {
        private views.SplashScreen? _splashScreen;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show splash screen
            _splashScreen = new views.SplashScreen();
            _splashScreen.Show();

            try
            {
                // Load all data
                await AppDataService.Instance.LoadAllDataAsync();
                
                // Small delay so user sees "ready" status
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка загрузки данных:\n{ex.Message}\n\nПриложение продолжит работу без данных.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // Show main window
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            // Close splash
            _splashScreen.Close();
            _splashScreen = null;
        }
    }
}
