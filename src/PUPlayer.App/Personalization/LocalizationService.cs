using System.Globalization;
using System.Windows;

namespace PUPlayer.App.Personalization;

public interface ITextProvider { string Text(string key, params object[] arguments); }

public sealed class LocalizationService(ResourceDictionary resources) : ITextProvider
{
    private ResourceDictionary? active;

    public void Apply(string language)
    {
        language = language == "es" ? "es" : "en";
        var dictionary = new ResourceDictionary { Source = new($"/IgoonTube;component/Localization/Strings.{language}.xaml", UriKind.Relative) };
        if (active is not null) resources.MergedDictionaries.Remove(active);
        resources.MergedDictionaries.Insert(0, dictionary);
        active = dictionary;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
    }

    public string Text(string key, params object[] arguments)
    {
        var value = resources[key]?.ToString() ?? key;
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentUICulture, value, arguments);
    }
}
