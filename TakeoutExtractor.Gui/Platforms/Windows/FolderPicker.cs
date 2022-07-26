using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// Based on code from MauiFolderPickerSample https://github.com/jfversluis/MauiFolderPickerSample
// and https://blog.verslu.is/maui/folder-picker-with-dotnet-maui/ by Gerald Versluis.
// Licenced Attribution-ShareAlike 4.0 International (CC BY-SA 4.0) https://creativecommons.org/licenses/by-sa/4.0/

using WindowsFolderPicker = Windows.Storage.Pickers.FolderPicker;

namespace uk.andyjohnson.TakeoutExtractor.Gui.Platforms.Windows
{
    /// <summary>
    /// Windows-specific folder picker
    /// </summary>
    public class FolderPicker
    {
        /// <summary>
        /// Display a folder picker dialog.
        /// </summary>
        /// <returns>Selected directory, or null if no directory selected.</returns>
        public static async Task<DirectoryInfo?> PickFolderAsync()
        {
            var folderPicker = new WindowsFolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            // Get the current window's HWND by passing in the Window object and associate the HWND with the file picker
            var hwnd = ((MauiWinUIWindow)App.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            // Display picker
            try
            {
                var result = await folderPicker.PickSingleFolderAsync();
                return result != null ? new DirectoryInfo(result.Path) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }    
    }
}
