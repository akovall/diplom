using System;
using System.Windows;
using diplom.viewmodels;

namespace diplom.views
{
    public partial class UserAnalyticsDialog : Window
    {
        public UserAnalyticsDialog()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (DataContext is AnalyticsViewModel vm)
            {
                vm.RefreshChartTheme();
            }
        }
    }
}
