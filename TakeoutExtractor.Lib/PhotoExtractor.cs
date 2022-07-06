using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

using ExifLibrary;


namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Photo/video extractor.
    /// </summary>
    public class PhotoExtractor
    {
        /// <summary>
        /// Coonstructor. Initialise a PhotoExtractor object.
        /// </summary>
        /// <param name="options">Photo options</param>
        /// <param name="inputDir">Root input directory</param>
        /// <param name="outputDir">Root output directory</param>
        /// <param name="verbosity">Lverbosity level. 0 = quiet.</param>
        public PhotoExtractor(
            PhotoOptions options,
            DirectoryInfo inputDir,
            DirectoryInfo outputDir,
            int verbosity = 0)
        {
            this.inputDir = inputDir;
            this.outputDir = outputDir;
            this.options = options;
            this.verbosity = verbosity;
        }

        private PhotoOptions options;
        private DirectoryInfo inputDir;
        private DirectoryInfo outputDir;
        private int verbosity;


        /// <summary>
        /// Progress event.
        /// </summary>
        public event EventHandler<string>? Progress;


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        public void Extract()
        {
            // Validate options.
            // If keeping originals then, to prevent file name clash, subdir and/or suffix option must be set.
            if (options.KeepOriginalsForEdited && string.IsNullOrEmpty(options.OriginalsSubdirName) && string.IsNullOrEmpty(options.OriginalsSuffix))
                throw new InvalidOperationException("Original file subdir and/or suffix option must be set to prevent file name clash");


            // Do it.
            var progressInfo = new ProgressInfo();
            ProcessDir(inputDir, outputDir, options, progressInfo);

            var duration = DateTime.UtcNow - progressInfo.startTime; ;
            RaiseProgress(1, $"Done in {duration.ToString("hh\\:mm\\:ss")}");

            RaiseProgress(2, $"Input groups: {progressInfo.inputGroupCount}");
            RaiseProgress(2, $"    Edited: {progressInfo.inputEditedCount}");
            RaiseProgress(2, $"    Un-edited: {progressInfo.inputUneditedCount}");
            RaiseProgress(2, $"Output files: {progressInfo.outputFileCount}");
            RaiseProgress(2, $"    Edited + original: {progressInfo.outputEditedCount}");
            RaiseProgress(2, $"    Un-edited: {progressInfo.outputUneditedCount}");
            RaiseProgress(2, $"Coverage: {progressInfo.Coverage * 100M}%");
        }



        #region Implementation

        private const string starDotJason = "*.json";

        private class ProgressInfo
        {
            public DateTime startTime = DateTime.UtcNow;

            public int inputGroupCount;
            public int inputEditedCount;
            public int inputUneditedCount;

            public int outputFileCount;
            public int outputEditedCount;  // + original
            public int outputUneditedCount;

            public decimal Coverage
            { 
                get { return this.inputGroupCount  != 0 ? (decimal)this.outputFileCount / (decimal)this.inputGroupCount : 0M; } 
            }
        }



        private void ProcessDir(
            DirectoryInfo inDir,
            DirectoryInfo outDir,
            PhotoOptions options,
            ProgressInfo progressInfo)
        {
            // Process all json manifests in 'inDir'
            foreach (var jsonManifestFile in inDir.EnumerateFiles(starDotJason))
            {
                var mi = ExtractManifestInfo(jsonManifestFile);
                if (mi == null)
                {
                    // Not a manifest file
                    continue;
                }
                if ((mi.originalFile == null) || !mi.originalFile.Exists)
                {
                    throw new InvalidOperationException($"Failed to identify original content file for {jsonManifestFile}");
                }

                progressInfo.inputGroupCount += 1;
                if (mi.editedFile != null)
                    progressInfo.inputEditedCount += 1;
                else
                    progressInfo.inputUneditedCount += 1;

                DirectoryInfo destDir = outDir;
                if (mi.originalFile.IsImageFile())
                    destDir = destDir.CreateSubdirectory("Photos");
                else if (mi.originalFile.IsVideoFile())
                    destDir = destDir.CreateSubdirectory("Videos");

                if (mi.editedFile != null)
                {
                    // We have an edited file, and an original file too.

                    // Create edited.
                    var outFile = CreateOutputFile(mi.editedFile, destDir, null, mi.description, mi.creationTime, mi.lastModifiedTime);
                    RaiseProgress(1, "{0} => {1}", mi.editedFile, outFile);
                    progressInfo.outputFileCount += 1;

                    // Optionally create original. 
                    if (options.KeepOriginalsForEdited)
                    {
                        if (!string.IsNullOrEmpty(options.OriginalsSubdirName))
                            destDir = destDir.CreateSubdirectory(options.OriginalsSubdirName);
                        outFile = CreateOutputFile(mi.originalFile, destDir, options.OriginalsSuffix, mi.description, mi.creationTime, mi.creationTime);
                        RaiseProgress(1, "{0} => {1}", mi.originalFile, outFile);
                        progressInfo.outputEditedCount += 1;
                    }
                }
                else
                {
                    // We have an original file only.
                    var outFile = CreateOutputFile(mi.originalFile, destDir, null, mi.description, mi.creationTime, mi.creationTime);
                    RaiseProgress(1, "{0} => {1}", mi.originalFile, outFile);
                    progressInfo.outputFileCount += 1;
                    progressInfo.outputUneditedCount += 1;
                }
            }

            // Recurse all subdirectories of 'inDir'.
            foreach (var subDir in inDir.EnumerateDirectories())
            {
                ProcessDir(subDir, outDir, options, progressInfo);
            }
        }


        private class ManifestInfo
        {
            public FileInfo originalFile = default!;        // Original media file name. Required.
            public FileInfo? editedFile;                    // Edited version, if there is one.

            public string title = default!;                 // Original filename. Required.
            public string? description;                     // Used-rovided description. Optional
            public DateTime creationTime;                   // UTC, required
            public DateTime? lastModifiedTime;              // UTC, can be null
        }


        /// <summary>
        /// Given a json metadata file, return information about the corresponding media file and edited version.
        /// </summary>
        /// <param name="jsonManifestFile">Path to the json metadata file.</param>
        /// <returns>
        /// If the json file is a media manifest the returns a ManifestInfo object containing path to the correspondng
        /// media original and edited media files, the media creation time, and last modified time. These times are UTC.
        /// Media file paths can be null if the media files do not exist.
        /// Timestamps can be null if metadata was not available.
        /// If the json file is not a media manifest the returns null.
        /// </returns>
        private ManifestInfo? ExtractManifestInfo(
            FileInfo jsonManifestFile)
        {
            /* const */ var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            const int maxFileNameLen = 47;

            var mi = new ManifestInfo();

            using (var manifestStm = new FileStream(jsonManifestFile.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var manifestDoc = JsonDocument.Parse(manifestStm, new JsonDocumentOptions() { AllowTrailingCommas = true }))
                {
                    // Get title from json manifest. Original file will be the same as the title
                    // but the name (excluding path and ext) will have a max length of 'maxFileNameLen' chars.
                    JsonElement elem;
                    if (manifestDoc.RootElement.TryGetProperty("title", out elem))
                    {
                        mi.title = elem.GetString()!;
                        mi.originalFile = new FileInfo(Path.Combine(jsonManifestFile.Directory!.ToString(), elem.GetString()!)).TrimName(maxFileNameLen);
                    }
                    else
                    {
                        // Not a manifest file.
                        return null;
                    }

                    if (manifestDoc.RootElement.TryGetProperty("description", out elem))
                    {
                        mi.description = elem.GetString();
                    }

                    // Get timestamps.
                    if (manifestDoc.RootElement.TryGetProperty("photoTakenTime", out elem) &&
                        elem.TryGetProperty("timestamp", out elem))
                    {
                        var ticksStr = long.Parse(elem.GetString()!);
                        mi.creationTime = unixEpoch.AddSeconds(ticksStr);
                    }
                    if (manifestDoc.RootElement.TryGetProperty("photoLastModifiedTime", out elem) &&
                        elem.TryGetProperty("timestamp", out elem))
                    {
                        var ticksStr = long.Parse(elem.GetString()!);
                        mi.lastModifiedTime = unixEpoch.AddSeconds(ticksStr);
                    }
                }
            }

            // If we found an original file then look for an edited version that is identified by a suffix.
            /* const */ string[] editedFileSuffixes = new string[] { "-edited", "-edite", "-edit", "-edi" };
            if (mi.originalFile != null)
            {
                foreach (var suffix in editedFileSuffixes)
                {
                    var ins = mi.originalFile.Name.LastIndexOf('.');
                    if (ins != -1)
                    {
                        var t = new FileInfo(Path.Combine(mi.originalFile.DirectoryName!, mi.originalFile.Name.Insert(ins, suffix)));
                        if (t.Exists)
                        {
                            mi.editedFile = t;
                            break;
                        }
                    }
                }
            }

            if (mi.creationTime.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException($"Bad time kind for image creation timestamp: {mi.creationTime.Kind}");
            if ((mi.lastModifiedTime != null) && (mi.lastModifiedTime.Value.Kind != DateTimeKind.Utc))
                throw new InvalidOperationException($"Bad time kind for image modification timestamp: {mi.lastModifiedTime.Value.Kind}");

            return mi;
        }



        /// <summary>
        /// Create an output file.
        /// </summary>
        /// <param name="sourceFile">Source file</param>
        /// <param name="outDir">Directory to create file in.</param>
        /// <param name="filenameSuffix">File name suffix. Can be null.</param>
        /// <param name="description">Image description. Can be null.</param>
        /// <param name="creationTime">Creation time. Must not null.</param>
        /// <param name="lastModifiedTime">Last modified time. Can be null.</param>
        /// <returns>FileInfo object referring to the created file.</returns>
        /// <exception cref="InvalidOperationException">File creation failed</exception>
        private FileInfo CreateOutputFile(
            FileInfo sourceFile,
            DirectoryInfo outDir,
            string? filenameSuffix,
            string? description,
            DateTime creationTime,
            DateTime? lastModifiedTime)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));
            if (!sourceFile.Exists)
                throw new ArgumentException("Source file does not exist", nameof(sourceFile));
            if (outDir == null)
                throw new ArgumentNullException(nameof(outDir));
            if (!outDir.Exists)
                throw new ArgumentException("Output directory does not exist", nameof(sourceFile));

            for (int i = 0; i < 9999; i++)
            {
                // Build the output file name. Because images can have timestamps that differ by less than a second, we prepend
                // a uniqieness suffix if necessary to make the filename uniqie.
                var uniqSuffix = (i == 0) ? "" : string.Format("-{0:000}", i);
                var filename = creationTime.ToString(this.options.OutputFileNameFormat) + uniqSuffix + filenameSuffix + sourceFile.Extension;
                var destFile = new FileInfo(Path.Combine(outDir.FullName, filename));
                if (destFile.Exists)
                {
                    continue;
                }
                File.Copy(sourceFile.FullName, destFile.FullName, false);

                if (options.UpdateExif && destFile.IsImageFileWithExif())
                {
                    var file = ImageFile.FromFile(destFile.FullName);

                    file.Properties.Set(ExifTag.ImageDescription, description);
                    var d = new ExifDateTime(ExifTag.DateTime, creationTime);
                    file.Properties.Set(ExifTag.DateTimeOriginal, d);
                    if (lastModifiedTime.HasValue)
                    {
                        d = new ExifDateTime(ExifTag.DateTime, lastModifiedTime.Value);
                        file.Properties.Set(ExifTag.DateTime, d);
                    }

                    file.Save(destFile.FullName);
                }

                File.SetCreationTime(destFile.FullName, creationTime);
                if (lastModifiedTime.HasValue)
                    File.SetLastWriteTime(destFile.FullName, lastModifiedTime.Value);

                return destFile;
            }

            // If we get to here then we were unable to generate a unique file name.
            throw new InvalidOperationException($"File uniqueness counter overflow for {sourceFile} in {outDir}");
        }


        public void RaiseProgress(
            int verbosityLevel,
            string message,
            params object[] parms)
        {
            if ((verbosityLevel >= this.verbosity) && (this.Progress != null))
            {
                this.Progress(this, string.Format(message, parms));
            }
        }

        #endregion Implementation
    }
}
