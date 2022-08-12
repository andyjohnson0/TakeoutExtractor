using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    /// <summary>
    /// Folder picker. Wraps platform specific implementations.
    /// </summary>
    public static class FolderPicker
    {
        /// <summary>
        /// Display a folder picker dialog.
        /// </summary>
        /// <returns>Selected directory, or null if no directory selected.</returns>
        public static async Task<DirectoryInfo?> PickFolderAsync()
        {
#if WINDOWS
            return await Platforms.Windows.FolderPicker.PickFolderAsync();
#elif MACCATALYST
            return await Platforms.MacCatalyst.FolderPicker.PickFolderAsync();
#else
            await Task.CompletedTask;  // prevents a warning about some code paths not doing an await
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
