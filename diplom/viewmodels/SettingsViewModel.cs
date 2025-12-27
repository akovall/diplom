using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace diplom.viewmodels
{
    public class SettingsViewModel : ObservableObject
    {
        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value) && value != null)
                {
                    LocalizationService.SetLanguage(value.Code);
                }
            }
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    ThemeService.SetTheme(value ? "Dark" : "Light");
                    OnPropertyChanged(nameof(ThemeIcon));
                }
            }
        }

        public string ThemeIcon => IsDarkTheme ? "🌙" : "☀️";

        public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new ObservableCollection<LanguageOption>();

        public ICommand ToggleThemeCommand { get; }

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
