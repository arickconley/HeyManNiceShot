using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace HeyManNiceShot.Settings;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        var s = App.Current.Settings;
        HotkeyInput.Text = s.CaptureHotkey.ToString();
        FolderInput.Text = s.SaveFolder;
        FormatCombo.SelectedIndex = s.DefaultFormat == ImageFormat.Jpeg ? 1 : 0;
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose screenshot folder",
            AllowMultiple = false,
        });
        if (folders.Count > 0)
        {
            var path = folders[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path)) FolderInput.Text = path;
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (!HotkeyChord.TryParse(HotkeyInput.Text, out var chord))
        {
            ErrorText.Text = "Invalid hotkey. Use modifiers + key, e.g. 'Ctrl+Shift+4'.";
            ErrorText.IsVisible = true;
            return;
        }

        var folder = FolderInput.Text ?? "";
        if (string.IsNullOrWhiteSpace(folder))
        {
            ErrorText.Text = "Save folder is required.";
            ErrorText.IsVisible = true;
            return;
        }

        var format = FormatCombo.SelectedIndex == 1 ? ImageFormat.Jpeg : ImageFormat.Png;
        var newSettings = new AppSettings(chord, folder, format);

        try
        {
            App.Current.ApplySettings(newSettings);
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not save settings: {ex.Message}";
            ErrorText.IsVisible = true;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
