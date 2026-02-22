using System;
using System.Windows;
using diplom.viewmodels;

namespace diplom.views
{
    public partial class ProjectAnalyticsDialog : Window
    {
        public ProjectAnalyticsDialog()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (DataContext is ProjectAnalyticsViewModel vm)
            {
                vm.RefreshChartTheme();
            }
        }
    }
}
