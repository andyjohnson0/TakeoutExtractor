using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    public class PhotoResults : IExtractorResults
    {
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }

        public int InputGroupCount { get; set; }
        public int InputEditedCount { get; set; }
        public int InputUneditedCount { get; set; }

        public int OutputFileCount { get; set; }
        public int OutputEditedCount { get; set; }  // + original
        public int OutputUneditedCount { get; set; }

        public decimal Coverage
        {
            get { return this.InputGroupCount != 0 ? (decimal)this.OutputFileCount / (decimal)this.InputGroupCount : 0M; }
        }

        public void Add(ExtractorAlert alert)
        {
            alerts.Add(alert);
        }

        private List<ExtractorAlert> alerts = new List<ExtractorAlert>();

        public IEnumerable<ExtractorAlert> Alerts
        {
            get { return alerts; }
        }
    }
}
