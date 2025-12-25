using System;
using System.Linq;
using System.Windows;

namespace diplom.Services
{
    public static class ThemeService
    {
        public static string CurrentTheme { get; private set; } = "Light";

        public static void SetTheme(string themeName)
        {
            string uriString = $"/diplom;component/Assets/Themes/{themeName}.xaml";
            var uri = new Uri(uriString, UriKind.RelativeOrAbsolute);

            ResourceDictionary newThemeDict = new ResourceDictionary { Source = uri };

            var mergedDicts = Application.Current.Resources.MergedDictionaries;
       
            var oldThemeDict = mergedDicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Assets/Themes/"));

            if (oldThemeDict != null)
            {
                mergedDicts.Remove(oldThemeDict);
            }

            mergedDicts.Add(newThemeDict);

            CurrentTheme = themeName;
        }
    }
}
