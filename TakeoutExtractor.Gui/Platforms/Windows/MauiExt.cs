using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Gui.Platforms.Windows
{
    /// <summary>
    /// Windows-specific extensions to MAUI classes.
    /// </summary>
    internal static class WindowExt
    {
        /// <summary>
        /// Show a window in a given state.
        /// </summary>
        /// <param name="window">The window</param>
        /// <param name="state">The window state</param>
        /// <returns>True if the oprtation succeeded, otherwise false.</returns>
        public static bool ShowWindow(Window window, Gui.WindowExt.WindowState state)
        {
            var view = window?.Handler?.PlatformView as MauiWinUIWindow;
            if (view != null)
            {
                return ShowWindow(view.WindowHandle, (int)state);
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Flash the window caption and and taskbar icon
        /// </summary>
        /// <param name="window">he window</param>
        /// <param name="numFlashes">Number of flashes</param>
        /// <returns>True if the oprtation succeeded, otherwise false.</returns>
        public static bool FlashWindow(Window window, int numFlashes)
        {
            var view = window?.Handler?.PlatformView as MauiWinUIWindow;
            if (view != null)
            {
                var fw = new FLASHWINFO();
                fw.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(fw);
                fw.hwnd = view.WindowHandle;
                fw.dwFlags = (uint)FlashWindowFlags.FLASHW_ALL;
                fw.uCount = (uint)numFlashes;
                fw.dwTimeout = 0;  // default
                return FlashWindowEx(ref fw);
            }
            else
            {
                return false;
            }
        }


        #region Imports

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);



        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        private enum FlashWindowFlags : uint
        {
            /// <summary>
            /// Stop flashing. The system restores the window to its original state.
            /// </summary>    
            FLASHW_STOP = 0,

            /// <summary>
            /// Flash the window caption
            /// </summary>
            FLASHW_CAPTION = 1,

            /// <summary>
            /// Flash the taskbar button.
            /// </summary>
            FLASHW_TRAY = 2,

            /// <summary>
            /// Flash both the window caption and taskbar button.
            /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
            /// </summary>
            FLASHW_ALL = 3,

            /// <summary>
            /// Flash continuously, until the FLASHW_STOP flag is set.
            /// </summary>
            FLASHW_TIMER = 4,

            /// <summary>
            /// Flash continuously until the window comes to the foreground.
            /// </summary>
            FLASHW_TIMERNOFG = 12
        }

        #endregion Imports

    }
}
