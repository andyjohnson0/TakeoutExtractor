using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    /// <summary>
    /// Extensions to the Maui Layout class
    /// </summary>
    public static class LayoutExt
    {
        /// <summary>
        /// Recursively enables/disables a layout and its children
        /// </summary>
        /// <param name="self">The layoutr</param>
        /// <param name="isEnabled">Enable or disable></param>
        public static void SetEnabledAll(
            this Layout self,
            bool isEnabled)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            self.IsEnabled = isEnabled;
            foreach (var child in self.Children)
            {
                if (child is VisualElement cve)
                {
                    cve.IsEnabled = isEnabled;
                }

                if (child is Layout cl)
                {
                    cl.SetEnabledAll(isEnabled);
                }

                if (child is IContentView ccv)
                {
                    if (ccv.Content is VisualElement ccvve)
                    {
                        ccvve.IsEnabled = isEnabled;
                    }
                    if (ccv.Content is Layout ccvl)
                    {
                        ccvl.SetEnabledAll(isEnabled);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Extensions for Maui menu bar.
    /// SetEnabledAll() is used to recursively enable/disable menus. This is required because MenuBarItem.IsEnabled
    /// does not affect the enabled state of its children. See https://github.com/dotnet/maui/issues/11602.
    /// </summary>
    public static class MenuBarExt
    { 
        /// <summary>
        /// Disable a collection of MenuBarItem objects, including their children
        /// This can be used as a one-shot way to disable a ContentPage's menu bar.
        /// </summary>
        /// <param name="self">MenuBar collection</param>
        /// <param name="isEnabled">True to enable, false to disable</param>
        /// <exception cref="ArgumentNullException">Argument must not be null;</exception>
        public static void SetEnabledAll(
            this IEnumerable<MenuBarItem> self,
            bool isEnabled)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            foreach(MenuBarItem mbi in self)
            {
                if (mbi != null)
                {
                    mbi.IsEnabled = isEnabled;
                    (mbi as IEnumerable<IMenuElement>).SetEnabledAll(isEnabled);
                }
            }
        }


        /// <summary>
        /// Disable a collection of IMenuElement objects
        /// </summary>
        /// <param name="self">Menu element collection</param>
        /// <param name="isEnabled">True to enable, false to disable</param>
        /// <exception cref="ArgumentNullException">Argument must not be null;</exception>
        public static void SetEnabledAll(
            this IEnumerable<IMenuElement> self,
            bool isEnabled)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            foreach(IMenuElement mi in self)
            {
                if (mi != null)
                {
                    mi.SetEnabledAll(isEnabled);
                }
            }
        }


        /// <summary>
        /// Disable an IMenuElement object, includig its children
        /// </summary>
        /// <param name="self">Menu element</param>
        /// <param name="isEnabled">True to enable, false to disable</param>
        /// <exception cref="ArgumentNullException">Argument must not be null;</exception>
        public static void SetEnabledAll(
            this IMenuElement self,
            bool isEnabled)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (self is MenuBarItem mbi)
            {
                mbi.IsEnabled = isEnabled;
                (mbi as IEnumerable<IMenuElement>).SetEnabledAll(isEnabled);
            }
            else if (self is MenuFlyoutSubItem mfsi)
            {
                mfsi.IsEnabled = isEnabled;
                (mfsi as IEnumerable<IMenuElement>).SetEnabledAll(isEnabled);
            }
            else if (self is MenuFlyoutItem mfi)
            {
                mfi.IsEnabled = isEnabled;
                // MenuFlyoutItem has no children
            }
        }
    }


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
