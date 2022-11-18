using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Photo
{
    /// <summary>
    /// Options for PhotoExtractor
    /// </summary>
    public class PhotoOptions : IExtractorOptions
    {
        /// <summary>
        /// Format of output file name.
        /// </summary>
        public string OutputFileNameFormat { get; set; } = "yyyyMMdd_HHmmss";

        /// <summary>
        /// Kind of date/time used doe output file name.
        /// If value is Vnspecified then local time kind is used.
        /// </summary>
        public DateTimeKind OutputFileNameTimeKind { get; set; } = DateTimeKind.Local;

        /// <summary>
        /// Kinds of output organisation.
        /// </summary>
        public enum OutputFileOrganisation
        {
            /// <summary>Place all output files in the same directory.</summary>
            None = 0,
            /// <summary>Create seperate directors for each year.</summary>
            Year,
            /// <summary>Create seperate directors for each year, and month sub-directories.</summary>
            YearMonth,
            /// <summary></summary>
            /// <summary>Create seperate directors for each year and month, and day sub-directories.</summary>
            YearMonthDay
        }

        /// <summary>
        /// Store output files in directories by date/time of creation
        /// </summary>
        public OutputFileOrganisation OrganiseBy { get; set; } = OutputFileOrganisation.None;

        /// <summary>
        /// Extract and retain original photo/video files if there is an edited version.
        /// </summary>
        public bool KeepOriginalsForEdited { get; set; } = false;

        // File name suffix for original ophoto/video files.
        public string? OriginalsSuffix { get; set; } = "_original";

        /// <summary>
        /// Subdirectory name for original ophoto/video files.
        /// </summary>
        public string? OriginalsSubdirName { get; set; } = "original";

        /// <summary>
        /// Update image exif information using values in json manifest.
        /// </summary>
        public bool UpdateExif { get; set; } = true;

        /// <summary>
        /// Default values
        /// </summary>
        public static readonly PhotoOptions Defaults = new PhotoOptions()
        {
            OutputFileNameFormat = "yyyyMMdd_HHmmss",
            OutputFileNameTimeKind = DateTimeKind.Local,
            OrganiseBy = OutputFileOrganisation.None,
            KeepOriginalsForEdited = false,
            OriginalsSuffix = "_original",
            OriginalsSubdirName = "original",
            UpdateExif = true
        };


        /// <summary>
        /// Validate options.
        /// TODO: This mixes implementation and UI a bit. Fefactor?
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Vaildate()
        {
            try
            {
                DateTime.UtcNow.ToString(OutputFileNameFormat);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Invalid output format option");
            }
            if (KeepOriginalsForEdited && string.IsNullOrEmpty(OriginalsSuffix) && string.IsNullOrEmpty(OriginalsSubdirName))
            {
                throw new InvalidOperationException("Originals suffix and/or originals subdirectory must be specified");
            }
        }
    }
}
