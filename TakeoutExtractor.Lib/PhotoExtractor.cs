using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

using ExifLibrary;
using System.Diagnostics;


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
        /// <param name="logFileWtr">Log file writer. Can be null.</param>
        public PhotoExtractor(
            PhotoOptions options,
            DirectoryInfo inputDir,
            DirectoryInfo outputDir,
            Utf8JsonWriter? logFileWtr)
        {
            this.options = options;
            this.inputDir = inputDir;
            this.outputDir = outputDir;
            this.logFileWtr = logFileWtr;
        }

        private PhotoOptions options;
        private DirectoryInfo inputDir;
        private DirectoryInfo outputDir;
        private Utf8JsonWriter? logFileWtr;


        /// <summary>
        /// Progress event.
        /// </summary>
        public event EventHandler<ProgressEventArgs>? Progress;


        /// <summary>
        /// An ExtractorAlert has been generated.
        /// </summary>
        public event EventHandler<ExtractorAlertEventArgs>? Alert;



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

            if (logFileWtr != null)
            {
                logFileWtr.WriteStartObject("PhotosAndVideos");
                logFileWtr.WriteStartArray("ExtractedFiles");
            }

            // Do it.
            var results = new PhotoResults();
            results.StartTime = DateTime.UtcNow;
            await ProcessDirAsync(inputDir, outputDir, options, results, cancellationToken);
            results.Duration = DateTime.UtcNow - results.StartTime;

            if (logFileWtr != null)
            {
                logFileWtr.WriteEndArray();

                logFileWtr.WriteStartObject("Results");
                logFileWtr.WriteString("TimeTaken", results.Duration.ToString("hh\\:mm\\:ss"));
                logFileWtr.WriteNumber("InputGroupTotalCount", results.InputGroupCount);
                logFileWtr.WriteNumber("InputGroupEditedCount", results.InputEditedCount);
                logFileWtr.WriteNumber("InputGroupUneditedCount", results.InputUneditedCount);
                logFileWtr.WriteNumber("OutputTotalFileCount", results.OutputFileCount);
                logFileWtr.WriteNumber("OutputEditedFileCount", results.OutputEditedCount);
                logFileWtr.WriteNumber("OutputUneditedFileCount", results.OutputUneditedCount);
                logFileWtr.WriteNumber("CoveragePercent", results.Coverage * 100M);
                logFileWtr.WriteEndObject();

                logFileWtr.WriteEndObject();
            }

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
                    var alert = new ExtractorAlert(ExtractorAlertType.Error, $"Failed to identify original content file for {jsonManifestFile}")
                    {
                        AssociatedFile = jsonManifestFile
                    };
                    results.Add(alert);
                    RaiseAlert(alert);
                    continue;
                }

                results.InputGroupCount += 1;
                if (mi.editedFile != null)
                    results.InputEditedCount += 1;
                else
                    results.InputUneditedCount += 1;

                // Create output directory in necessary.
                DirectoryInfo destDir = CreateOutputDir(outDir, mi, options);

                if (mi.editedFile != null)
                {
                    // We have an edited file, and an original file too.

                    // Create edited.
                    var outFile = await CreateOutputFileAsync(mi.editedFile, destDir, null, mi.description, mi.creationTime, mi.lastModifiedTime, results);
                    RaiseProgress(mi.editedFile, outFile);
                    results.OutputFileCount += 1;

                    // Optionally create original. 
                    if (options.KeepOriginalsForEdited)
                    {
                        if (!string.IsNullOrEmpty(options.OriginalsSubdirName))
                            destDir = destDir.CreateSubdirectory(options.OriginalsSubdirName);
                        outFile = await CreateOutputFileAsync(mi.originalFile, destDir, options.OriginalsSuffix, mi.description, mi.creationTime, mi.creationTime, results);
                        RaiseProgress(mi.originalFile, outFile);
                        results.OutputEditedCount += 1;
                    }
                }
                else
                {
                    // We have an original file only.
                    var outFile = await CreateOutputFileAsync(mi.originalFile, destDir, null, mi.description, mi.creationTime, mi.creationTime, results);
                    RaiseProgress(mi.originalFile, outFile);
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


        /// <summary>
        /// Create and return the output directory given the file media type, creation time, and options.
        /// </summary>
        /// <param name="outputDir">Base output directory</param>
        /// <param name="mi">Manifest information</param>
        /// <param name="options">Options.</param>
        /// <returns>The created output directory.</returns>
        private static DirectoryInfo CreateOutputDir(
            DirectoryInfo outputDir,
            ManifestInfo mi,
            PhotoOptions options)
        {
            if (mi.originalFile.IsImageFile())
            {
                outputDir = outputDir.CreateSubdirectory("Photos");
            }
            else if (mi.originalFile.IsVideoFile())
            {
                outputDir = outputDir.CreateSubdirectory("Videos");
            }
            else
            {
                outputDir = outputDir.CreateSubdirectory("Unknown");
            }

            if (options.OrganiseBy == PhotoOptions.OutputFileOrganisation.Year ||
                options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonth ||
                options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonthDay)
            {
                outputDir = outputDir.CreateSubdirectory(mi.creationTime.Year.ToString("D4"));
            }
            if (options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonth ||
                options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonthDay)
            {
                outputDir = outputDir.CreateSubdirectory(mi.creationTime.Month.ToString("D2"));
            }
            if (options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonthDay)
            {
                outputDir = outputDir.CreateSubdirectory(mi.creationTime.Day.ToString("D2"));
            }
            return outputDir;
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
                        var fn = mi.title.Replace('&', '_').Replace('?', '_');
                        if (Path.GetExtension(fn) == String.Empty)
                            fn = fn + ".jpg";
                        mi.originalFile = new FileInfo(Path.Combine(jsonManifestFile.Directory!.ToString(), fn)).TrimName(maxFileNameLen);
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
                        mi.creationTime = unixEpoch.AddSeconds(ticksStr);  // UTC
                    }
                    if (manifestDoc.RootElement.TryGetProperty("photoLastModifiedTime", out elem) &&
                        elem.TryGetProperty("timestamp", out elem))
                    {
                        var ticksStr = long.Parse(elem.GetString()!);
                        mi.lastModifiedTime = unixEpoch.AddSeconds(ticksStr);  // UTC
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
        /// <param name="results">Results object. Must not be null.</param>
        /// <returns>FileInfo object referring to the created file.</returns>
        /// <exception cref="InvalidOperationException">File creation failed</exception>
        private async Task<FileInfo> CreateOutputFileAsync(
            FileInfo sourceFile,
            DirectoryInfo outDir,
            string? filenameSuffix,
            string? description,
            DateTime creationTime,
            DateTime? lastModifiedTime,
            PhotoResults results)
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
                    try
                    {
                        var imageFile = await ImageFile.FromFileAsync(destFile.FullName);
                        imageFile.Properties.Set(ExifTag.ImageDescription, description);
                        var d = new ExifDateTime(ExifTag.DateTime, creationTime);
                        imageFile.Properties.Set(ExifTag.DateTimeOriginal, d);
                        if (lastModifiedTime.HasValue)
                        {
                            d = new ExifDateTime(ExifTag.DateTime, lastModifiedTime.Value);
                            imageFile.Properties.Set(ExifTag.DateTime, d);
                        }
                        await imageFile.SaveAsync(destFile.FullName);
                        if (imageFile.Errors?.Count > 0)
                        {
                            var imageAlert = new ExtractorAlert(ImageErrorsToAlertType(imageFile.Errors), "One or more errors occurred while updatimg image EXIF data")
                            {
                                AssociatedFile = destFile,
                                AssociatedObject = imageFile.Errors
                            };
                            results.Add(imageAlert);
                            RaiseAlert(imageAlert);
                        }
                    }
                    catch(Exception ex)
                    {
                        var exifAlert = new ExtractorAlert(ExtractorAlertType.Error, "An error occurred while updating image EXIF data")
                        {
                            AssociatedFile = destFile,
                            AssociatedException = ex
                        };
                        results.Add(exifAlert);
                        RaiseAlert(exifAlert);
                    }
                }

                File.SetCreationTime(destFile.FullName, creationTime);
                if (lastModifiedTime.HasValue)
                    File.SetLastWriteTime(destFile.FullName, lastModifiedTime.Value);

                if (logFileWtr != null)
                {
                    logFileWtr.WriteStartObject();
                    if (sourceFile.IsImageFile())
                        logFileWtr.WriteString("Type", "Photo");
                    else if (sourceFile.IsVideoFile())
                        logFileWtr.WriteString("Type", "Video");
                    else
                        logFileWtr.WriteString("Type", "Unknown");
                    logFileWtr.WriteString("Source", sourceFile.FullName);
                    logFileWtr.WriteString("Output", destFile.FullName);
                    logFileWtr.WriteString("CreationTime", creationTime.ToString("u"));
                    if (lastModifiedTime.HasValue)
                        logFileWtr.WriteString("ModifiedTime", lastModifiedTime.Value.ToString("u"));
                    else
                        logFileWtr.WriteNull("ModifiedTime");
                    logFileWtr.WriteEndObject();
                }

                return destFile;
            }

            // If we get to here then we were unable to generate a unique file name.
            var alert = new ExtractorAlert(ExtractorAlertType.Error, "Couldb't create a unique output filename")
            {
                AssociatedDirectory = outDir,
                AssociatedFile = sourceFile
            };
            RaiseAlert(alert);
            throw new InvalidOperationException();
        }


        private static ExtractorAlertType ImageErrorsToAlertType(IEnumerable<ImageError> errors)
        {
            if (errors.Any(e => e.Severity == Severity.Error))
                return ExtractorAlertType.Error;
            else if (errors.Any(e => e.Severity == Severity.Warning))
                return ExtractorAlertType.Warning;
            else
                return ExtractorAlertType.Information;
        }


        private void RaiseProgress(
            FileInfo sourceFile,
            FileInfo destinationFile)
        {
            if (this.Progress != null)
            {
                var args = new ProgressEventArgs(sourceFile, destinationFile);
                this.Progress(this, args);
            }
        }


        private void RaiseAlert(
            ExtractorAlert alert)
        {
            if (logFileWtr != null)
            {
                alert.Write(logFileWtr);
            }

            if (this.Alert != null)
            {
                var args = new ExtractorAlertEventArgs(alert);
                this.Alert(this, args);
            }
        }


        #endregion Implementation
    }
}
