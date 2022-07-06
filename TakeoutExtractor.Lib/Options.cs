using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Base class for options
    /// </summary>
    public abstract class Options
    {
        /// <summary>
        /// Validate options.
        /// </summary>
        public abstract void Vaildate();
    }
}
