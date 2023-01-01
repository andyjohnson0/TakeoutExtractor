using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UIKit;


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
            if (optionLabels.Length == 0)
                throw new ArgumentException(nameof(optionLabels));

            // UIKit's UIAlertController seems to be intrinsically non-modal with no way to map it onto
            // a blocking, on-shot method api - or to run the UI thread until a button is pressed.
            // So here I just use Page.DisplayAlert() and impose an arbitrary maximum of two button labels.
            // Not great but it works...

            if (optionLabels.Length == 1)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert(title, content, optionLabels[0]);
                return optionLabels[0];
            }
            else if (optionLabels.Length == 2)
            {
                var b = await Application.Current!.Windows[0].Page!.DisplayAlert(title, content,
                                                                                 optionLabels[0], optionLabels[1]);
                return b ? optionLabels[0] : optionLabels[1];
            }
            else
            {
                throw new NotImplementedException("Too many option labels. Maximum is 2.");
            }
        }
    }
}
