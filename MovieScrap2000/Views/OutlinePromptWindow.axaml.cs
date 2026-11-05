using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using static VideoScrap2000.Localization.Locale;

namespace VideoScrap2000.Views;

public partial class OutlinePromptWindow : Window
{
    public OutlinePromptWindow() : this("") { }

    public OutlinePromptWindow(string prompt)
    {
        InitializeComponent();
        Title = T("Kurzbeschreibung mit KI erzeugen");
        Instructions.Text = T("Öffne eine KI, zum Beispiel ChatGPT. Kopiere den gesamten Text unten und füge ihn dort ein. Kopiere anschließend das Ergebnis und füge es im Tool in das Feld „Kurzbeschreibung“ ein.");
        PromptText.Text = prompt;
        CopyButton.Content = T("Alles kopieren");
        CloseButton.Content = T("Schließen");
        Opened += async (_, _) =>
        {
            await CopyPromptAsync();
            PromptText.Focus();
            PromptText.SelectAll();
        };
    }

    private async Task CopyPromptAsync()
    {
        try
        {
            if (Clipboard is not { } clipboard)
            {
                CopyStatus.Text = T("Bitte den Text markieren und manuell kopieren.");
                return;
            }
            await clipboard.SetTextAsync(PromptText.Text ?? "");
            CopyStatus.Text = T("Der Text wurde in die Zwischenablage kopiert.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            CopyStatus.Text = T("Bitte den Text markieren und manuell kopieren.");
        }
    }

    private async void CopyPrompt(object? sender, RoutedEventArgs e) => await CopyPromptAsync();

    private async void OpenChatGpt(object? sender, RoutedEventArgs e)
    {
        await CopyPromptAsync();
        try
        {
            if (!await Launcher.LaunchUriAsync(new Uri("https://chatgpt.com/")))
                CopyStatus.Text = T("ChatGPT konnte nicht im Browser geöffnet werden.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            CopyStatus.Text = T("ChatGPT konnte nicht im Browser geöffnet werden.");
        }
    }

    private void CloseWindow(object? sender, RoutedEventArgs e) => Close();
}
