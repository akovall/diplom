using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace diplom.viewmodels
{
    public partial class SettingsViewModel : ObservableObject
    {
        // === Profile ===
        public string Username => ApiClient.Instance.Username;
        public string Role => ApiClient.Instance.Role;
        public string UserInitials => GetInitials(ApiClient.Instance.FullName);

        private string _fullName = ApiClient.Instance.FullName ?? string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _jobTitle = ApiClient.Instance.JobTitle ?? string.Empty;
        public string JobTitle
        {
            get => _jobTitle;
            set => SetProperty(ref _jobTitle, value);
        }

        [ObservableProperty]
        private bool _isEditingProfile;

        // === Theme ===
        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    // Defer ResourceDictionary swap to after the click event chain completes.
                    // Without this, the swap happens mid-click and WPF reverts IsChecked.
                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        () => ThemeService.SetTheme(value ? "Dark" : "Light"));
                    OnPropertyChanged(nameof(ThemeIcon));
                }
            }
        }

        public string ThemeIcon => IsDarkTheme ? "🌙" : "☀️";

        // === Language ===
        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value) && value != null)
                    LocalizationService.SetLanguage(value.Code);
            }
        }

        public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new();

        // === Commands ===
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public IAsyncRelayCommand SaveProfileCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ChangePasswordCommand { get; }

        public SettingsViewModel()
        {
            AvailableLanguages.Add(new LanguageOption { Code = "uk", Name = "Українська", Flag = "🇺🇦" });
            AvailableLanguages.Add(new LanguageOption { Code = "en", Name = "English", Flag = "🇬🇧" });

            _isDarkTheme = ThemeService.CurrentTheme == "Dark";

            var currentLang = LocalizationService.CurrentLanguage;
            foreach (var lang in AvailableLanguages)
            {
                if (lang.Code == currentLang)
                {
                    _selectedLanguage = lang;
                    break;
                }
            }

            ToggleThemeCommand = new RelayCommand(() => IsDarkTheme = !IsDarkTheme);
            LogoutCommand = new RelayCommand(Logout);
            EditProfileCommand = new RelayCommand(() => IsEditingProfile = true);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SaveProfileCommand = new AsyncRelayCommand(SaveProfileAsync);
            ChangePasswordCommand = new RelayCommand(ChangePassword);
        }

        private void CancelEdit()
        {
            FullName = ApiClient.Instance.FullName;
            JobTitle = ApiClient.Instance.JobTitle;
            IsEditingProfile = false;
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                var success = await ApiClient.Instance.PutAsync("/api/auth/profile", new { FullName, JobTitle });
                if (success)
                {
                    ApiClient.Instance.FullName = FullName;
                    ApiClient.Instance.JobTitle = JobTitle;
                    IsEditingProfile = false;
                    OnPropertyChanged(nameof(UserInitials));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update profile: " + ex.Message);
            }
        }

        private void ChangePassword()
        {
            MessageBox.Show("Change password feature coming soon.");
        }

        private void Logout()
        {
            ApiClient.Instance.Logout();
            System.Diagnostics.Process.Start(
                System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!);
            Application.Current.Shutdown();
        }

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : fullName[0].ToString().ToUpper();
        }
    }

    public class LanguageOption
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Flag { get; set; }

        public string DisplayName => $"{Flag} {Name}";
    }
}
