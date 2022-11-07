using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace andyjohnson.uk.TakeoutExtractor.Gui
{
    /// <summary>
    /// Extensions to the MAUI Window class.
    /// </summary>
    public static class WindowExt
    {
        /// <summary>
        /// Window states. Values map to Windows WS_xxxx values defined at https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
        /// </summary>
        public enum WindowState
        {
            Maximised = 3,
        };


        /// <summary>
        /// Show a window in a given state.
        /// </summary>
        /// <param name="self">The window</param>
        /// <param name="state">The window state</param>
        /// <returns>True if the oprtation succeeded, otherwise false.</returns>
        public static bool ShowWindow(
            this Window self, 
            WindowState state)
        {
#if WINDOWS
            return Platforms.Windows.WindowExt.ShowWindow(self, state);
#elif MACCATALYST
            return true;
#else
            throw new PlatformNotSupportedException();
#endif
        }


        /// <summary>
        /// Flash the window caption and and taskbar icon
        /// </summary>
        /// <param name="self">he window</param>
        /// <param name="numFlashes">Number of flashes</param>
        /// <returns>True if the oprtation succeeded, otherwise false.</returns>
        public static bool FlashWindow(
            this Window self, 
            int numFlashes)
        {
#if WINDOWS
            return Platforms.Windows.WindowExt.FlashWindow(self, numFlashes);
#elif MACCATALYST
            return true;
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
