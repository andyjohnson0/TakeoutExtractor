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
        public bool StopOnError { get; set; } = true;

        /// <summary>
        /// Create json log file
        /// </summary>
        public bool CreateLogFile { get; set; } = false;


        /// <summary>
        /// Default values
        /// </summary>
        public static readonly GlobalOptions Defaults = new GlobalOptions()
        {
            InputDir = null,
            OutputDir = null,
            CreateLogFile = false
        };
    }
}
