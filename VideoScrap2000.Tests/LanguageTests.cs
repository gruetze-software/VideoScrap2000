using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using MovieScrap2000;
using VideoScrap2000.Localization;
using VideoScrap2000.ViewModels;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;
public class LanguageTests
{
    [AvaloniaTheory]
    [InlineData("en", "Settings", "Room for great movies.")]
    [InlineData("de", "Einstellungen", "Platz für großes Kino.")]
    public void InterfaceUsesRequestedLanguage(string language, string settings, string empty)
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = language, MetadataLanguage = "en", ArtworkLanguage = "de" });
        var window = new MainWindow(model, false); window.Show();
        try
        {
            window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content?.ToString() == settings);
            Assert.Equal(empty, model.EmptyTitle);
            Assert.Equal("en-US", model.MetadataLanguage); Assert.Equal("de-DE", model.ArtworkLanguage);
            Assert.Equal(language == "en" ? "New" : "Neu", Locale.T("Neu"));
            Assert.Equal(language == "en" ? "Loaded 42 movies." : "42 Filme geladen.", Locale.T("42 Filme geladen."));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ExistingWindowLabelsChangeWhenLanguageChanges()
    {
        using var model = new MainViewModel(settings: new AppSettings { Language = "de" });
        var window = new MainWindow(model, false); window.Show();
        try
        {
            Locale.Apply("en"); window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content?.ToString() == "Settings");
            Locale.Apply("de"); window.UpdateLayout();
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content?.ToString() == "Einstellungen");
        }
        finally { window.Close(); }
    }
    [Theory]
    [InlineData("en", "de", "en-US")]
    [InlineData("de", "en", "de-DE")]
    [InlineData("System", "en", "en-US")]
    [InlineData("System", "de", "de-DE")]
    public void ExplicitLanguageOverridesFollowingInterface(string preference, string ui, string expected) => Assert.Equal(expected, LanguagePreferences.Resolve(preference, ui));
}
