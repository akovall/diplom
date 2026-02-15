using System.Windows;
using System.Windows.Input;

namespace diplom.views
{
    public partial class CreateTaskDialog : Window
    {
        public bool DialogResultOk { get; private set; } = false;

        public CreateTaskDialog()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResultOk = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResultOk = false;
            DialogResult = false;
            Close();
        }
    }
}
