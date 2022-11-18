using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    public class GlobalOptions
    {
        /// <summary>
        /// Root input directory
        /// </summary>
        public DirectoryInfo? InputDir { get; set; }

        /// <summary>
        /// Root output directory
        /// </summary>
        public DirectoryInfo? OutputDir { get; set; }

        /// <summary>
        /// Stop when an error occurs
        /// </summary>
        public bool StopOnError { get; set; } = false;

        /// <summary>
        /// Type of log file.
        /// </summary>
        public enum LogFileType
        {
            None = 0,
            Json,
            Xml
        };

        /// <summary>
        /// Type of log file to create.
        /// </summary>
        public LogFileType LogFile { get; set; } = GlobalOptions.LogFileType.None;

        /// <summary>
        /// Default values
        /// </summary>
        public static readonly GlobalOptions Defaults = new GlobalOptions()
        {
            InputDir = null,
            OutputDir = null,
            StopOnError = false,
            LogFile = LogFileType.None
        };
    }
}
