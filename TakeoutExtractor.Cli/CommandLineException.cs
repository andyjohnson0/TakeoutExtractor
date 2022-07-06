using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Cli
{
    // Exception related to command-line handling
    public class CommandLineException : Exception
    {
        /// <summary>
        /// Constructor. Initailise a CommandLineException object.
        /// </summary>
        public CommandLineException()
            : base()
        {
        }

        /// <summary>
        /// Constructor. Initailise a CommandLineException object.
        /// </summary>
        /// <param name="message">Error description</param>
        public CommandLineException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructor. Initailise a CommandLineException object.
        /// </summary>
        /// <param name="message">Error description</param>
        /// <param name="innerException">Inner exception</param>
        public CommandLineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
