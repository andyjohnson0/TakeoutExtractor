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
        /// Create json log file
        /// </summary>
        public bool CreateLogFile { get; set; } = false;
    }
}
