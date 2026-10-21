using System.Windows;

namespace VMIdentityLab.App.Infrastructure;

public static class Localization
{
    public static bool English { get; private set; }
    public static string Text(string key) => Application.Current.Resources[key] as string ?? key;

    public static void Set(bool english)
    {
        English = english;
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var language = english ? "en-US" : "zh-CN";
        var dictionary = new ResourceDictionary { Source = new Uri(
            $"pack://application:,,,/VMIdentityLab.App;component/Resources/Strings.{language}.xaml", UriKind.Absolute) };
        if (dictionaries.Count > 1) dictionaries.RemoveAt(dictionaries.Count - 1);
        dictionaries.Add(dictionary);
    }
}
