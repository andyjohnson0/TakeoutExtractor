using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    public interface IExtractorResults
    {
        DateTime StartTime { get; set; }
        TimeSpan Duration { get; set; }

        decimal Coverage { get; }

        void Add(ExtractorAlert alert);

        IEnumerable<ExtractorAlert> Alerts { get; }
    }
}
