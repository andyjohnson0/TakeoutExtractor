using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Base interface for options
    /// </summary>
    public interface IExtractorOptions
    {
        /// <summary>
        /// Validate options.
        /// </summary>
        void Vaildate();
    }
}
