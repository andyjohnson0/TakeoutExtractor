using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Gui
{
    /// <summary>
    /// Question dialog. Wraps platform specific implementations.
    /// </summary>
    public static class QuestionDialog
    {
        /// <summary>
        /// Display a question dialog.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="content">Message</param>
        /// <param name="defaultOption">Label for default button.</param>
        /// <param name="extraOption">Label for additional optional button. Can be null.</param>
        /// <returns>Selected option</returns>
        public static async Task<string?> ShowAsync(
            string title,
            string content,
            string defaultOption,
            string? extraOption)
        {
            var optionLabels = (extraOption != null) ? new string[] { defaultOption, extraOption } : new string[] { defaultOption };
            return await ShowAsync(title, content, optionLabels);
        }


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
#if WINDOWS
            return await Platforms.Windows.QuestionDialog.ShowAsync(title, content, optionLabels);
#elif MACCATALYST
            return await Platforms.MacCatalyst.QuestionDialog.ShowAsync(title, content, optionLabels);
#else
            await Task.CompletedTask;  // prevents a warning about some code paths not doing an await
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
