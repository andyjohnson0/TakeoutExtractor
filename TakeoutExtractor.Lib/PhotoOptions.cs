using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Options for PhotoExtractor
    /// </summary>
    public class PhotoOptions : IExtractorOptions
    {
        // 
        /// <summary>
        /// Validate options.
        /// TODO: This mixes implementation and UI a bit. Fefactor?
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Vaildate()
        {
            try
            {
                DateTime.UtcNow.ToString(this.OutputFileNameFormat);
            }
            catch(Exception)
            {
                throw new InvalidOperationException("Invalid output format option");
            }
            if (this.KeepOriginalsForEdited && string.IsNullOrEmpty(this.OriginalsSuffix) && string.IsNullOrEmpty(this.OriginalsSubdirName))
            {
                throw new InvalidOperationException("Originals suffix and/or originals subdirectory must be specified");
            }
        }


        /// <summary>
        /// Format of output file name.
        /// </summary>
        public string OutputFileNameFormat { get; set; } = "yyyyMMdd_HHmmss";

        /// <summary>
        /// Extract and retain original photo/video files if there is an edited version.
        /// </summary>
        public bool KeepOriginalsForEdited { get; set; } = true;

        // File name suffix for original ophoto/video files.
        public string OriginalsSuffix { get; set; } = "_original";

        /// <summary>
        /// Subdirectory name for original ophoto/video files.
        /// </summary>
        public string OriginalsSubdirName { get; set; } = "original";

        /// <summary>
        /// Update image exif information using values in json manifest.
        /// </summary>
        public bool UpdateExif { get; set; } = true;
    }
}
