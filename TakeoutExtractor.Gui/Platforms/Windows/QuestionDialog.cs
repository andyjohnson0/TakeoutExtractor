using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;

namespace uk.andyjohnson.TakeoutExtractor.Gui.Platforms.Windows
{
    public static class QuestionDialog
    {
        /// <summary>
        /// Display a question dialog.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="content">Message</param>
        /// <param name="optionLabels">Button labels. Default option should be element 0.</param>
        /// <returns>Selected option</returns>
        public static async Task<string?> ShowAsync(
            string title,
            string content,
            params string[] optionLabels)
        {
            var dlg = new MessageDialog(content, title);
            var hwnd = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(dlg, hwnd);
            foreach (var optionLabel in optionLabels)
            {
                dlg.Commands.Add(new UICommand(optionLabel));
            };
            dlg.DefaultCommandIndex = 0;
            var result = await dlg.ShowAsync();
            return result?.Label;
        }
    }
}
