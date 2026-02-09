using diplom.Services;
using System.Windows;

namespace diplom.views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            
            // Subscribe to loading status changes
            AppDataService.Instance.LoadingStatusChanged += OnLoadingStatusChanged;
        }

        private void OnLoadingStatusChanged(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        protected override void OnClosed(System.EventArgs e)
        {
            AppDataService.Instance.LoadingStatusChanged -= OnLoadingStatusChanged;
            base.OnClosed(e);
        }
    }
}
