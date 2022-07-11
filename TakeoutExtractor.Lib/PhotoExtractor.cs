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
        public event EventHandler<ProgressEventArgs>? Progress;



        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PhotoResults object describing results</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public async Task<PhotoResults> ExtractAsync(CancellationToken cancellationToken)
        {
            // Validate options.
            // If keeping originals then, to prevent file name clash, subdir and/or suffix option must be set.
            if (options.KeepOriginalsForEdited && string.IsNullOrEmpty(options.OriginalsSubdirName) && string.IsNullOrEmpty(options.OriginalsSuffix))
                throw new InvalidOperationException("Original file subdir and/or suffix option must be set to prevent file name clash");

            // Do it.
            var results = new PhotoResults();
            results.StartTime = DateTime.UtcNow;
            await ProcessDirAsync(inputDir, outputDir, options, results, cancellationToken);
            results.Duration = DateTime.UtcNow - results.StartTime;

            //var duration = DateTime.UtcNow - results.StartTime;
            //RaiseProgress(1, $"Done in {duration.ToString("hh\\:mm\\:ss")}");
            //RaiseProgress(2, $"Input groups: {progressInfo.inputGroupCount}");
            //RaiseProgress(2, $"    Edited: {progressInfo.inputEditedCount}");
            //RaiseProgress(2, $"    Un-edited: {progressInfo.inputUneditedCount}");
            //RaiseProgress(2, $"Output files: {progressInfo.outputFileCount}");
            //RaiseProgress(2, $"    Edited + original: {progressInfo.outputEditedCount}");
            //RaiseProgress(2, $"    Un-edited: {progressInfo.outputUneditedCount}");
            //RaiseProgress(2, $"Coverage: {progressInfo.Coverage * 100M}%");

            return results;
        }


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PhotoResults object describing results</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public PhotoResults Extract(CancellationToken cancellationToken)
        {
            try
            {
                // Defer to the async version.
                return Task.Run(async () => await ExtractAsync(cancellationToken)).Result;
            }
            catch (AggregateException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                return null; // Not reached
            }
        }


        #region Implementation

        private const string starDotJason = "*.json";



        private async Task ProcessDirAsync(
            DirectoryInfo inDir,
            DirectoryInfo outDir,
            PhotoOptions options,
            PhotoResults results,
            CancellationToken cancellationToken)
        {
            // Process all json manifests in 'inDir'
            foreach (var jsonManifestFile in inDir.EnumerateFiles(starDotJason))
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                var mi = await ExtractManifestInfoAsync(jsonManifestFile);
                if (mi == null)
                {
                    // Not a manifest file
                    continue;
                }
                if ((mi.originalFile == null) || !mi.originalFile.Exists)
                {
                    throw new InvalidOperationException($"Failed to identify original content file for {jsonManifestFile}");
                }

                results.InputGroupCount += 1;
                if (mi.editedFile != null)
                    results.InputEditedCount += 1;
                else
                    results.InputUneditedCount += 1;

                DirectoryInfo destDir = outDir;
                if (mi.originalFile.IsImageFile())
                    destDir = destDir.CreateSubdirectory("Photos");
                else if (mi.originalFile.IsVideoFile())
                    destDir = destDir.CreateSubdirectory("Videos");

                if (mi.editedFile != null)
                {
                    // We have an edited file, and an original file too.

                    // Create edited.
                    var outFile = await CreateOutputFileAsync(mi.editedFile, destDir, null, mi.description, mi.creationTime, mi.lastModifiedTime);
                    RaiseProgress(1, mi.editedFile, outFile);
                    results.OutputFileCount += 1;

                    // Optionally create original. 
                    if (options.KeepOriginalsForEdited)
                    {
                        if (!string.IsNullOrEmpty(options.OriginalsSubdirName))
                            destDir = destDir.CreateSubdirectory(options.OriginalsSubdirName);
                        outFile = await CreateOutputFileAsync(mi.originalFile, destDir, options.OriginalsSuffix, mi.description, mi.creationTime, mi.creationTime);
                        RaiseProgress(1, mi.originalFile, outFile);
                        results.OutputEditedCount += 1;
                    }
                }
                else
                {
                    // We have an original file only.
                    var outFile = await CreateOutputFileAsync(mi.originalFile, destDir, null, mi.description, mi.creationTime, mi.creationTime);
                    RaiseProgress(1, mi.originalFile, outFile);
                    results.OutputFileCount += 1;
                    results.OutputUneditedCount += 1;
                }
            }

            // Recurse all subdirectories of 'inDir'.
            foreach (var subDir in inDir.EnumerateDirectories())
            {
                await ProcessDirAsync(subDir, outDir, options, results, cancellationToken);
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
        private async Task<ManifestInfo?> ExtractManifestInfoAsync(
            FileInfo jsonManifestFile)
        {
            /* const */ var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            const int maxFileNameLen = 47;

            var mi = new ManifestInfo();

            using (var manifestStm = new FileStream(jsonManifestFile.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var manifestDoc = await JsonDocument.ParseAsync(manifestStm, new JsonDocumentOptions() { AllowTrailingCommas = true }))
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
        private async Task<FileInfo> CreateOutputFileAsync(
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
                    var file = await ImageFile.FromFileAsync(destFile.FullName);

                    file.Properties.Set(ExifTag.ImageDescription, description);
                    var d = new ExifDateTime(ExifTag.DateTime, creationTime);
                    file.Properties.Set(ExifTag.DateTimeOriginal, d);
                    if (lastModifiedTime.HasValue)
                    {
                        d = new ExifDateTime(ExifTag.DateTime, lastModifiedTime.Value);
                        file.Properties.Set(ExifTag.DateTime, d);
                    }

                    await file.SaveAsync(destFile.FullName);
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
            FileInfo sourceFile,
            FileInfo destinationFile)
        {
            if ((verbosityLevel >= this.verbosity) && (this.Progress != null))
            {
                var args = new ProgressEventArgs(sourceFile, destinationFile);
                this.Progress(this, args);
            }
        }

        #endregion Implementation
    }
}
