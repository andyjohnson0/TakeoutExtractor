using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Photo
{
    /// <summary>
    /// 
    /// </summary>
    public enum PhotoFileVersionOrganisation
    {
        /// <summary>Latest version only</summary>
        LatestVersionOnly,
        /// <summary>Latest version with originals in sub-folder</summary>
        AllVersionsOriginalsSubdir,
        /// <summary>All versions in same folder</summary>
        AllVersionsSameDir,
        /// <summary>All versions each in separate sub-folders</summary>
        AllVersionsSeparateSubdirs,
        /// <summary>Edited versionss only</summary>
        EditedVersionsOnly,
        /// <summary>Original versions only</summary>
        OriginalVersionsOnly
    }

    /// <summary>
    /// Kinds of output organisation.
    /// </summary>
    public enum PhotoDirOrganisation
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
        /// Organise output files by version.
        /// </summary>
        public PhotoFileVersionOrganisation OutputFileVersionOrganisation { get; set; } = PhotoFileVersionOrganisation.LatestVersionOnly;


        /// <summary>
        /// Organise directories by date/time of creation
        /// </summary>
        public PhotoDirOrganisation OutputDirOrganisation { get; set; } = PhotoDirOrganisation.None;


        /// <summary>
        /// Update image exif information using values in json manifest.
        /// </summary>
        public bool UpdateExif { get; set; } = true;


        /// <summary>
        /// Extract the deleted file in the bin directory.
        /// </summary>
        public bool ExtractDeletedFiles { get; set; } = false;


        /// <summary>
        /// Default values
        /// </summary>
        public static readonly PhotoOptions Defaults = new PhotoOptions()
        {
            OutputFileNameFormat = "yyyyMMdd_HHmmss",
            OutputFileNameTimeKind = DateTimeKind.Local,
            OutputFileVersionOrganisation = PhotoFileVersionOrganisation.LatestVersionOnly,
            OutputDirOrganisation = PhotoDirOrganisation.None,
            UpdateExif = true,
            ExtractDeletedFiles = false
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
        }
    }
}
