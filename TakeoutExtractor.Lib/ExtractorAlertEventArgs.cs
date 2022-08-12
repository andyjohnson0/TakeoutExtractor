using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Event args for an ExtractorAlert event
    /// </summary>
    public class ExtractorAlertEventArgs
    {
        /// <summary>
        /// Cibstructor. Initailise an ExtractorAlertEventArgs object.
        /// </summary>
        /// <param name="alert">The alert object</param>
        public ExtractorAlertEventArgs(
            ExtractorAlert alert)
        {
            this.Alert = alert;
        }

        /// <summary>
        /// The alert object.
        /// </summary>
        public readonly ExtractorAlert Alert;
    }
}
