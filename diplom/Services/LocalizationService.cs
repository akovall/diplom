using System;
using System.Windows;

namespace diplom.Services
{
    public static class LocalizationService
    {
        public static string CurrentLanguage { get; private set; } = "uk";

        public static event Action LanguageChanged;

        public static void SetLanguage(string language)
        {
            if (language != "uk" && language != "en")
                language = "uk";

            CurrentLanguage = language;

            var dict = new ResourceDictionary();
            dict.Source = new Uri($"/Assets/Localization/Strings.{language}.xaml", UriKind.Relative);

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            for (int i = mergedDicts.Count - 1; i >= 0; i--)
            {
                var source = mergedDicts[i].Source;
                if (source != null && source.OriginalString.Contains("/Localization/"))
                {
                    mergedDicts.RemoveAt(i);
                }
            }

            mergedDicts.Add(dict);

            LanguageChanged?.Invoke();
        }

        public static string GetString(string key)
        {
            if (Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key] as string ?? key;
            }
            return key;
        }
    }
}
