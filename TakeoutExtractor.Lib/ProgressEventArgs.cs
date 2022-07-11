using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Extraction progress information
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(
            FileInfo sourceFile,
            FileInfo desctinationFile)
        {
            this.SourceFile = sourceFile;
            this.DesinationFile = desctinationFile;
        }

        /// <summary>
        /// Source (input) file
        /// </summary>
        public FileInfo SourceFile { get; private set; }

        /// <summary>
        /// Destination (output) file
        /// </summary>
        public FileInfo DesinationFile { get; private set; }
    }
}
