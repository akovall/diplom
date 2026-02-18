using diplom.Services;
using System;
using System.Windows;

namespace diplom.views
{
    public partial class LoginWindow : Window
    {
        private bool _isRegisterMode = false;

        public bool IsLoggedIn { get; private set; } = false;

        public LoginWindow()
        {
            InitializeComponent();
            UsernameBox.Focus();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password");
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = _isRegisterMode ? "Registering..." : "Logging in...";
            HideError();

            try
            {
                if (_isRegisterMode)
                {
                    var fullName = FullNameBox.Text.Trim();
                    var jobTitle = JobTitleBox.Text.Trim();

                    if (string.IsNullOrEmpty(fullName))
                    {
                        ShowError("Please enter full name");
                        return;
                    }

                    var (success, error) = await ApiClient.Instance.RegisterAsync(
                        username, password, fullName, jobTitle);

                    if (!success)
                    {
                        ShowError($"Registration error: {error}");
                        return;
                    }
                }
                else
                {
                    var success = await ApiClient.Instance.LoginAsync(username, password);
                    if (!success)
                    {
                        ShowError("Invalid username or password");
                        return;
                    }
                }

                IsLoggedIn = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Connection error: {ex.Message}");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = _isRegisterMode ? "Register" : "Login";
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;

            if (_isRegisterMode)
            {
                RegisterFields.Visibility = Visibility.Visible;
                LoginButton.Content = "Register";
                ToggleText.Text = "Already have an account?";
                ToggleButton.Content = "Login";
            }
            else
            {
                RegisterFields.Visibility = Visibility.Collapsed;
                LoginButton.Content = "Login";
                ToggleText.Text = "Don't have an account?";
                ToggleButton.Content = "Register";
            }

            HideError();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = "";
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}
