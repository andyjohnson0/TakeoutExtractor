using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Gui.Platforms.MacCatalyst
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
            await Task.CompletedTask;  // prevents a warning about not doing an await
            throw new NotImplementedException();
        }
    }
}
