using Microsoft.Win32;
using System.IO;

namespace PUPlayer.App.MediaTools;

public sealed class SaveFileClipDestinationPicker : IClipDestinationPicker
{
    public string? Pick(string defaultPath)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".mp4",
            Filter = "MP4 video|*.mp4",
            FileName = Path.GetFileName(defaultPath),
            InitialDirectory = Path.GetDirectoryName(defaultPath),
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
